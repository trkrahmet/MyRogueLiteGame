using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] int maxHp = 3;
    [SerializeField] int contactDamage = 1;

    int hp;
    Transform player;
    Rigidbody2D rb;

    void Start()
    {
        hp = maxHp;
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void FixedUpdate()
    {
        if (player == null) return;
        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0) Destroy(gameObject);
        Debug.Log("damaged");
    }
}
