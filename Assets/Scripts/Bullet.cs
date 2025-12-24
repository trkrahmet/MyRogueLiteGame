using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] float speed = 12f;
    [SerializeField] float lifeTime = 2f;
    [SerializeField] int damage = 1;

    Vector2 dir;

    public void SetDirection(Vector2 direction)
    {
        dir = direction.normalized;
    }

    void Start()
    {
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
        if (enemy != null)
            enemy.TakeDamage(damage);

        Destroy(gameObject);
    }
}
