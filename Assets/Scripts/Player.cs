using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 6f;

    [Header("Health")]
    public int maxHp = 10;
    public int currentHp;

    [Header("Bullet")]
    public GameObject bulletPrefab;
    public Transform firePoint; // boş child objesi (player'ın üstünde)
    public float fireRate = 0.3f;
    float shootTimer;

    [Header("Knockback")]
    public float knockbackStrength = 6f;
    public float knockbackDuration = 0.12f;

    [Header("Invulnerability (i-frames)")]
    public float invulnDuration = 0.35f;

    Rigidbody2D rb;
    Vector2 inputMove;

    Vector2 knockbackVel;
    float knockbackTimer;

    float invulnTimer;

    void Awake()
    {
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
        b.GetComponent<Bullet>().SetDirection(dir);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        // i-frame aktifse görmezden gel
        if (invulnTimer > 0f) return;

        // Hasar
        TakeDamage(1);

        // Knockback (enemy'den uzak yöne)
        Vector2 dir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        knockbackVel = dir * knockbackStrength;
        knockbackTimer = knockbackDuration;

        // i-frame başlat
        invulnTimer = invulnDuration;
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
        // Şimdilik basit: sahneyi yeniden başlat
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
}
