using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private bool destroyOnHit = true;

    private Rigidbody2D rb;
    private Collider2D myCol;

    private Transform owner;
    private Collider2D ownerCol;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();
        myCol.isTrigger = true; // top-down için en stabil
    }

    private void OnEnable()
    {
        if (lifeTime > 0f) Invoke(nameof(SelfDestruct), lifeTime);
    }

    private void OnDisable()
    {
        CancelInvoke();
        rb.linearVelocity = Vector2.zero;

        // Owner collision ignore'u geri aç (pool kullanırsan önemli)
        if (ownerCol != null)
            Physics2D.IgnoreCollision(myCol, ownerCol, false);

        owner = null;
        ownerCol = null;
    }

    public void Fire(Vector2 direction, Transform ownerTransform)
    {
        owner = ownerTransform;

        // owner ile çarpışmayı kapat (boss kendi mermisine asla çarpmaz)
        if (owner != null)
        {
            ownerCol = owner.GetComponent<Collider2D>();
            if (ownerCol != null)
                Physics2D.IgnoreCollision(myCol, ownerCol, true);
        }

        rb.linearVelocity = direction.normalized * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Owner'a çarpma (ek güvenlik)
        if (owner != null && other.transform == owner) return;

        // Sadece player
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponentInParent<Player>();
        if (player != null)
            player.TakeDamage(damage);

        if (destroyOnHit)
            SelfDestruct();
    }

    private void SelfDestruct()
    {
        Destroy(gameObject);
        // Pool kullanacaksan: gameObject.SetActive(false);
    }
}

