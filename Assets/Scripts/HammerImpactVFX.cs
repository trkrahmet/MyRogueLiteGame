using UnityEngine;

public class HammerImpactVFX : MonoBehaviour
{
    [SerializeField] private LineRenderer lr;
    [SerializeField] private int segments = 64;

    [Header("Timing")]
    [SerializeField] private float lifetime = 0.14f;

    [Header("Ring")]
    [SerializeField] private float startRadius = 0.25f;
    [SerializeField] private float endRadius = 1.05f;
    [SerializeField] private float startWidth = 0.18f;
    [SerializeField] private float endWidth = 0.03f;

    float t;

    void Awake()
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        lr.positionCount = segments;
        lr.loop = true;
        lr.useWorldSpace = true;
    }

    public void Play(Vector2 center)
    {
        transform.position = center;
        t = 0f;
        UpdateRing(0f);
    }

    void Update()
    {
        t += Time.deltaTime / lifetime;
        UpdateRing(Mathf.Clamp01(t));

        if (t >= 1f) Destroy(gameObject);
    }

    void UpdateRing(float u)
    {
        float radius = Mathf.Lerp(startRadius, endRadius, EaseOut(u));
        float width = Mathf.Lerp(startWidth, endWidth, u);

        lr.startWidth = width;
        lr.endWidth = width;

        float step = (Mathf.PI * 2f) / segments;
        for (int i = 0; i < segments; i++)
        {
            float a = step * i;
            Vector3 p = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            lr.SetPosition(i, (Vector3)transform.position + p);
        }
    }

    float EaseOut(float x) => 1f - Mathf.Pow(1f - x, 3f);
}
