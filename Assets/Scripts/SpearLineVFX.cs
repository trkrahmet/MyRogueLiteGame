using UnityEngine;

public class SpearLineVFX : MonoBehaviour
{
    [SerializeField] private LineRenderer lr;
    [SerializeField] private float lifetime = 0.07f;

    [Header("Width")]
    [SerializeField] private float mainWidth = 0.14f;   // kalın!
    [SerializeField] private float tipWidth = 0.02f;    // uç incelsin

    float t;

    void Awake()
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        // taper: ortası kalın, ucu ince
        var curve = new AnimationCurve(
            new Keyframe(0f, tipWidth),
            new Keyframe(0.15f, mainWidth),
            new Keyframe(0.85f, mainWidth),
            new Keyframe(1f, tipWidth)
        );
        lr.widthCurve = curve;
    }

    public void Play(Vector2 start, Vector2 end)
    {
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        t = 0f;
    }

    void Update()
    {
        t += Time.deltaTime / lifetime;

        // minik “flash”: ilk yarıda biraz büyüsün sonra sönsün
        float pulse = (t < 0.5f) ? Mathf.Lerp(1f, 1.25f, t / 0.5f) : Mathf.Lerp(1.25f, 1f, (t - 0.5f) / 0.5f);
        lr.widthMultiplier = pulse;

        if (t >= 1f) Destroy(gameObject);
    }
}
