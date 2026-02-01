using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] GameObject xpOrbPrefab;
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] int maxHp = 3;
    int hp;
    public int contactDamage = 1;
    [SerializeField] float hitFlashDuration = 0.06f;
    Color originalColor;
    float hitFlashTimer;

    [Header("AI")]
    [SerializeField] private bool enableChaseMovement = true;


    [Header("Death Pop")]
    [SerializeField] float deathPopDuration = 0.12f;
    [SerializeField] float deathPopScale = 1.15f;

    private bool isDead = false;
    float deathPopTimer;
    Vector3 baseScale;

    Transform playerTransform;
    Rigidbody2D rb;
    SpriteRenderer sr;

    public int CurrentHp => hp;
    public int CurrentMaxHp => _scaledMaxHp;

    private int _scaledMaxHp;
    public event System.Action<int, int> OnHealthChanged;


    void Start()
    {
        hp = maxHp;
        _scaledMaxHp = maxHp;
        OnHealthChanged?.Invoke(hp, _scaledMaxHp);

        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        originalColor = sr.color;
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;

            if (hitFlashTimer <= 0f)
                sr.color = originalColor;
        }

        if (isDead)
        {
            deathPopTimer -= Time.deltaTime;
            float t = 1f - (deathPopTimer / deathPopDuration); // 0->1

            // önce hafif büyü, sonra küçül
            float s = (t < 0.35f)
                ? Mathf.Lerp(1f, deathPopScale, t / 0.35f)
                : Mathf.Lerp(deathPopScale, 0f, (t - 0.35f) / 0.65f);

            transform.localScale = baseScale * s;

            // opsiyonel: aynı anda biraz saydamlaşsın (sprite beyazsa güzel durur)
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                sr.color = c;
            }

            if (deathPopTimer <= 0f)
                Destroy(gameObject);

            return; // ölürken başka update yapma
        }

    }

    void FixedUpdate()
    {
        if (!enableChaseMovement) return;   // <-- kritik satır
        if (playerTransform == null) return;

        Vector2 dir = ((Vector2)playerTransform.position - rb.position).normalized;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);

        if (dir.x != 0f && sr != null)
            sr.flipX = dir.x < 0f;
    }


    public void TakeDamage(int amount)
    {
        if (isDead) return;
        hp -= amount;
        OnHealthChanged?.Invoke(hp, _scaledMaxHp);
        sr.color = new Color(1f, 0.5f, 0.5f); // hafif kırmızı
        hitFlashTimer = hitFlashDuration;

        if (hp <= 0)
        {
            isDead = true;
            Instantiate(xpOrbPrefab, transform.position, Quaternion.identity);
            Die();
        }
    }

    public void InitForWave(int wave, float hpMult, float dmgMult, float speedMult)
    {
        int scaledMax = Mathf.Max(1, Mathf.RoundToInt(maxHp * hpMult));
        _scaledMaxHp = scaledMax;
        hp = scaledMax;
        OnHealthChanged?.Invoke(hp, _scaledMaxHp);


        contactDamage = Mathf.Max(1, Mathf.RoundToInt(contactDamage * dmgMult));
        moveSpeed *= speedMult;
    }


    void Die()
    {
        isDead = true;
        deathPopTimer = deathPopDuration;

        FindFirstObjectByType<GameManager>()?.RegisterEnemyKill();
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            // Elite fazındayken ölen “final düşman” elite’dir
            // (sahada tek düşman var dediğin için bu yaklaşım yeterli)
            gm.RegisterEliteKilled();

            // hareketi durdur
            if (rb != null) rb.simulated = false;

            // collider varsa kapat (2D)
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }
}
