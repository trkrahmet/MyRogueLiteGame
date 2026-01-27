using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] GameObject hitFxPrefab;
    private Player player;

    [SerializeField] float speed = 12f;
    [SerializeField] float lifeTime = 2f;
    public int damage = 1;

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
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        int finalDamage = damage;

        // 🎯 CRITICAL HIT CHECK
        if (player != null)
        {
            float roll = Random.value * 100f;
            if (roll <= player.critChance)
            {
                finalDamage = Mathf.RoundToInt(damage * player.critMultiplier);
            }
        }

        enemy.TakeDamage(finalDamage);

        // 💥 HIT VFX (aynı şekilde korunuyor)
        if (hitFxPrefab != null)
        {
            Vector2 offset = Random.insideUnitCircle * 0.08f;
            Instantiate(hitFxPrefab, (Vector2)transform.position + offset, Quaternion.identity);
        }

        Destroy(gameObject);
    }

}
