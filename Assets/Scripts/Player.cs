using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    // public List<WeaponSlot> weaponSlots = new List<WeaponSlot>();

    [Header("Movement")]
    Rigidbody2D rb;
    Vector2 inputMove;
    public float moveSpeed = 6f;

    [Header("Health")]
    public int maxHp = 10;
    public int currentHp;

    [Header("Combat")]
    Bullet bullet;
    public GameObject bulletPrefab;
    public Transform firePoint; // boş child objesi (player'ın üstünde)
    public int damage = 1;
    public float fireRate = 0.3f;
    float shootTimer;

    [Header("Knockback")]
    public float knockbackStrength = 6f;
    public float knockbackDuration = 0.12f;

    [Header("Invulnerability (i-frames)")]
    public float invulnDuration = 0.35f;
    Vector2 knockbackVel;
    float knockbackTimer;
    float invulnTimer;

    [Header("XP & Leveling")]
    public int playerLevel = 1;
    public int currentXp = 0;
    public int xpToNextLevel = 5;

    void Awake()
    {
        bullet = FindFirstObjectByType<Bullet>();
        rb = GetComponent<Rigidbody2D>();
        currentHp = maxHp;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputMove = new Vector2(h, v).normalized;

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            ShootNearestEnemy();
            shootTimer = fireRate;
        }
    }

    void FixedUpdate()
    {
        // i-frame timer
        if (invulnTimer > 0f)
            invulnTimer -= Time.fixedDeltaTime;

        // knockback timer
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
                knockbackVel = Vector2.zero;
        }

        Vector2 finalVel = (inputMove * moveSpeed) + knockbackVel;
        rb.MovePosition(rb.position + finalVel * Time.fixedDeltaTime);
    }

    void ShootNearestEnemy()
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return;

        Transform nearest = null;
        float bestDist = float.MaxValue;
        Vector2 myPos = transform.position;

        foreach (var e in enemies)
        {
            float d = Vector2.SqrMagnitude((Vector2)e.transform.position - myPos);
            if (d < bestDist)
            {
                bestDist = d;
                nearest = e.transform;
            }
        }

        if (nearest == null) return;

        Vector2 dir = ((Vector2)nearest.position - (Vector2)firePoint.position).normalized;

        GameObject b = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet == null) return; // prefab'da Bullet script'i yoksa sessizce çık

        bullet.damage = damage;
        bullet.SetDirection(dir);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("XP"))
        {
            currentXp += 1;
            Destroy(other.gameObject);
            TryLevelUp();
            return;
        }

        if (!other.CompareTag("Enemy")) return;

        // i-frame aktifse görmezden gel
        if (invulnTimer > 0f) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        // Hasar
        TakeDamage(enemy.contactDamage);

        // Knockback (enemy'den uzak yöne)
        Vector2 dir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        knockbackVel = dir * knockbackStrength;
        knockbackTimer = knockbackDuration;

        // i-frame başlat
        invulnTimer = invulnDuration;
    }

    private void TryLevelUp()
    {
        while (currentXp >= xpToNextLevel)
        {
            playerLevel += 1;
            currentXp -= xpToNextLevel;
            xpToNextLevel += 5; // sonraki seviye için gereken XP artışı
            ApplyRandomUpgrade();
        }
    }

    private void ApplyRandomUpgrade()
    {
        int choice = Random.Range(0, 4);
        switch (choice)
        {
            case 0:
                maxHp += 2;
                currentHp += 2;
                Debug.Log("Max HP increased!");
                break;
            case 1:
                moveSpeed += 1f;
                Debug.Log("Move Speed increased!");
                break;
            case 2:
                fireRate = Mathf.Max(0.1f, fireRate - 0.05f);
                Debug.Log("Fire Rate increased!");
                break;
            case 3:
                damage += 1;
                Debug.Log("Damage increased!");
                break;
        }
    }

    void TakeDamage(int amount)
    {
        currentHp -= amount;
        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }
    }

    void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
