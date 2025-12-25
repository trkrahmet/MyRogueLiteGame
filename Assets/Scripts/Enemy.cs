using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] GameObject xpOrbPrefab;
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] int maxHp = 3;
    public int contactDamage = 1;

    int hp;
    Transform playerTransform;
    Rigidbody2D rb;
    Player player;

    void Start()
    {
        hp = maxHp;
        rb = GetComponent<Rigidbody2D>();
        player = FindFirstObjectByType<Player>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;
        Vector2 dir = ((Vector2)playerTransform.position - rb.position).normalized;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0)
        {
            Instantiate(xpOrbPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
