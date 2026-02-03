using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] GameObject hitFxPrefab;
    [SerializeField] private GameObject critHitFxPrefab;   // ✅ crit için ayrı efekt
    [SerializeField] private float critFxScale = 1.25f;    // opsiyonel, biraz büyüt

    private Player player;

    [SerializeField] float speed = 12f;
    [SerializeField] float lifeTime = 2f;
    public int damage = 1;

    private Vector2 startPos;
    private float maxRange = 999f;
    private bool rangeSet = false;

    Vector2 dir;

    public void SetDirection(Vector2 direction)
    {
        dir = direction.normalized;
    }

    void Start()
    {
        player = FindFirstObjectByType<Player>();
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        if (rangeSet)
        {
            float traveled = Vector2.Distance(startPos, transform.position);
            if (traveled >= maxRange)
                Destroy(gameObject);
        }
    }

    public void SetMaxRange(float range)
    {
        maxRange = Mathf.Max(0.1f, range);
        startPos = transform.position;
        rangeSet = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        int finalDamage = damage;
        bool isCrit = false;

        // 🎯 CRITICAL HIT CHECK
        if (player != null)
        {
            float roll = Random.value * 100f;
            if (roll <= player.critChance)
            {
                isCrit = true;
                finalDamage = Mathf.RoundToInt(damage * player.critMultiplier);
            }
        }

        enemy.TakeDamage(finalDamage);

        // 💥 VFX
        GameObject fx = isCrit ? critHitFxPrefab : hitFxPrefab;

        if (fx != null)
        {
            Vector2 offset = Random.insideUnitCircle * 0.08f;
            GameObject go = Instantiate(fx, (Vector2)transform.position + offset, Quaternion.identity);

            if (isCrit && critFxScale > 0f)
                go.transform.localScale *= critFxScale;
        }

        Destroy(gameObject);
    }

    public void IgnoreCollider(Collider2D col)
    {
        var myCol = GetComponent<Collider2D>();
        if (myCol != null && col != null)
            Physics2D.IgnoreCollision(myCol, col);
    }
}
