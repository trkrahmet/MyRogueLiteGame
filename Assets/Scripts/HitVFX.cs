using UnityEngine;

public class HitVFX : MonoBehaviour
{
    [SerializeField] float duration = 0.12f;
    [SerializeField] float startScale = 0.6f;
    [SerializeField] float endScale = 1.1f;

    float t;

    void OnEnable()
    {
        t = 0f;
        transform.localScale = Vector3.one * startScale;
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }

    void Update()
    {
        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / duration);

        // pop büyüme
        float s = Mathf.Lerp(startScale, endScale, p);
        transform.localScale = Vector3.one * s;

        // yok olurken alpha düşür
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, p);
            sr.color = c;
        }

        if (t >= duration)
            Destroy(gameObject);
    }
}
