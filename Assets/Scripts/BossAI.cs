using UnityEngine;

public class BossAI : MonoBehaviour
{
    public enum BossMode { Chaser, PatrolShooter }

    [Header("Mode")]
    public BossMode mode = BossMode.Chaser;

    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D rb;

    [Header("Chaser")]
    [SerializeField] private float chaseSpeed = 1.8f;

    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 1.2f;
    [SerializeField] private float patrolRadius = 4f;   // sağ-sol sınır
    private Vector2 patrolCenter;
    private int patrolDir = 1;

    [Header("Shoot")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootInterval = 1.2f;
    private float shootTimer;

    [Header("Flip")]
    [SerializeField] private SpriteRenderer sr;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        if (player == null)
        {
            var p = FindFirstObjectByType<Player>();
            if (p != null) player = p.transform;
        }

        patrolCenter = rb.position;
        shootTimer = shootInterval;
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        Vector2 vel = Vector2.zero;

        if (mode == BossMode.Chaser)
        {
            Vector2 dir = ((Vector2)player.position - rb.position).normalized;
            vel = dir * chaseSpeed;
        }
        else // PatrolShooter
        {
            // sağ-sol devriye
            float targetX = patrolCenter.x + patrolDir * patrolRadius;
            float dx = targetX - rb.position.x;

            if (Mathf.Abs(dx) < 0.2f) patrolDir *= -1;

            vel = new Vector2(Mathf.Sign(dx) * patrolSpeed, 0f);

            // ateş
            shootTimer -= Time.fixedDeltaTime;
            if (shootTimer <= 0f)
            {
                shootTimer = shootInterval;
                ShootAtPlayer();
            }
        }

        rb.MovePosition(rb.position + vel * Time.fixedDeltaTime);

        // flip
        if (sr != null && vel.x != 0f)
            sr.flipX = vel.x < 0f;
    }

    private void ShootAtPlayer()
    {
        if (bulletPrefab == null) return;

        Vector2 dir = ((Vector2)player.position - rb.position).normalized;

        GameObject b = Instantiate(bulletPrefab, rb.position, Quaternion.identity);
        var bullet = b.GetComponent<Bullet>(); // sende Bullet var ama enemy bullet ayrı istiyorsan sonra ayırırız
        if (bullet != null)
            bullet.SetDirection(dir);

        // Not: Bu Bullet scriptin Enemy’yi vuruyor olabilir.
        // Boss'un mermisi için ayrı "EnemyBullet" yapman daha doğru (sonra).
    }
}
