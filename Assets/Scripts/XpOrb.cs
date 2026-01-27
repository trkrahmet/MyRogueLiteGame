using UnityEngine;

public class XpOrb : MonoBehaviour
{
    Transform player;
    private Player playerComp;

    [SerializeField] float attractRadius = 5f;
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] float stopDistance = 0.2f;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerComp = p.GetComponent<Player>(); // ✅
        }
    }

    void Update()
    {
        if (player == null) return;

        float bonus = (playerComp != null) ? playerComp.pickupRangeBonus : 0f;
        float effectiveRadius = attractRadius + bonus; // ✅ pickup range

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > effectiveRadius || dist <= stopDistance) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        float t = 1f - (dist / effectiveRadius);
        float speed = moveSpeed * Mathf.Clamp01(t);

        transform.position += (Vector3)(dir * speed * Time.deltaTime);
    }
}
