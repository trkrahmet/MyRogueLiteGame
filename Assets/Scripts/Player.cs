using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{
    [Serializable]
    public class WeaponSlot
    {
        public WeaponType type;
        public float interval;
        public float timer;
        public int damage;
        public int pelletCount;
        public float spreadAngle;
    }

    public enum WeaponType
    {
        Rifle,
        Shotgun
    }

    public List<WeaponSlot> weaponSlots = new List<WeaponSlot>();

    [Header("Player Stats")]
    public float moveSpeed = 6f;
    public int maxHp = 10;
    public int strength = 0;
    public int fireRate = 0;

    [Header("Movement")]
    Rigidbody2D rb;
    Vector2 inputMove;

    [Header("Health")]
    public int currentHp;

    [Header("Combat")]
    Bullet bullet;
    public GameObject bulletPrefab;
    public Transform firePoint;

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

        for (int i = 0; i < weaponSlots.Count; i++)
        {
            WeaponSlot slot = weaponSlots[i];

            slot.timer -= Time.deltaTime;
            if (slot.timer <= 0f)
            {
                Fire(slot);
                Debug.Log(slot.type + "fired!");
                slot.timer = Mathf.Max(0.1f, slot.interval - (fireRate * 0.05f));
            }
        }

        // shootTimer -= Time.deltaTime;
        // if (shootTimer <= 0f)
        // {
        //     ShootNearestEnemy();
        //     shootTimer = fireRate;
        // }
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

    Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return null;

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

        return nearest;
    }

    bool TryGetDirectionToNearest(out Vector2 dir)
    {
        dir = Vector2.zero;

        if (firePoint == null) return false;

        Transform target = FindNearestEnemy();
        if (target == null) return false;

        dir = ((Vector2)target.position - (Vector2)firePoint.position).normalized;
        return true;
    }

    void SpawnBullet(Vector2 dir, int damage)
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject b = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet == null) return;

        bullet.damage = damage + strength;
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
            // ApplyRandomUpgrade();
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
                for (int i = 0; i < weaponSlots.Count; i++)
                {
                    if (weaponSlots == null || weaponSlots.Count == 0) return;

                    weaponSlots[i].interval = Mathf.Max(0.1f, weaponSlots[i].interval - 0.05f);
                    Debug.Log("Fire Rate increased! (interval decreased)");
                }
                break;
            case 3:
                for (int i = 0; i < weaponSlots.Count; i++)
                {
                    if (weaponSlots == null || weaponSlots.Count == 0) return;

                    weaponSlots[i].damage += 1;
                    Debug.Log("Damage increased!");
                }
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

    void Fire(WeaponSlot slot)
    {
        switch (slot.type)
        {
            case WeaponType.Rifle:
                FireRifle(slot);
                break;

            case WeaponType.Shotgun:
                FireShotgun(slot);
                break;
        }
    }

    void FireRifle(WeaponSlot slot)
    {
        if (!TryGetDirectionToNearest(out Vector2 dir)) return;
        SpawnBullet(dir, slot.damage);
    }

    void FireShotgun(WeaponSlot slot)
    {
        if (!TryGetDirectionToNearest(out Vector2 dir)) return;

        int pellets = Mathf.Max(1, slot.pelletCount);

        // pelletCount 1 ise tek mermi gibi çalışsın
        if (pellets == 1)
        {
            SpawnBullet(dir, slot.damage);
            return;
        }

        float startAngle = -slot.spreadAngle / 2f;
        float step = slot.spreadAngle / (pellets - 1);

        for (int i = 0; i < pellets; i++)
        {
            float angle = startAngle + step * i;
            Vector2 rotatedDir = (Vector2)(Quaternion.Euler(0f, 0f, angle) * (Vector3)dir);

            SpawnBullet(rotatedDir, slot.damage);
        }
    }

    public void IncreaseStrength(int amount)
    {
        strength += amount;
    }

    public void IncreaseMoveSpeed(float amount)
    {
        moveSpeed += amount;
    }

    public void IncreaseFireRate(int amount)
    {
        fireRate += amount;
    }

    public void IncreaseMaxHealth(int amount)
    {
        maxHp += amount;
        currentHp += amount;
    }
}
