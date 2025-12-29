using UnityEngine;

public class XpOrb : MonoBehaviour
{
    Transform player;

    [SerializeField] float attractRadius = 5f;
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] float stopDistance = 0.2f;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > attractRadius || dist <= stopDistance) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        float t = 1f - (dist / attractRadius); // 0..1
        float speed = moveSpeed * Mathf.Clamp01(t);

        transform.position += (Vector3)(dir * speed * Time.deltaTime);
    }
}
