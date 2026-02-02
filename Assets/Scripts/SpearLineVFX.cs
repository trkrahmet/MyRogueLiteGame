using UnityEngine;

public class SpearLineVFX : MonoBehaviour
{
    [Header("Line Renderers")]
    [SerializeField] private LineRenderer core;
    [SerializeField] private LineRenderer glow;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 0.08f;

    [Header("Widths")]
    [SerializeField] private float coreWidth = 0.07f;   // merkez çizgi
    [SerializeField] private float glowWidth = 0.18f;   // dış parıltı
    [SerializeField] private float tipWidth = 0.01f;

    [Header("Colors")]
    [SerializeField] private Color coreColor = new Color(1f, 0.15f, 0.15f, 1f); // kırmızı
    [SerializeField] private Color glowColor = new Color(1f, 0.2f, 0.2f, 0.55f);

    [Header("Motion Feel")]
    [SerializeField] private float startGrow = 1.25f;   // ilk frame “patlama”
    [SerializeField] private float endShrink = 0.8f;    // sönüşte incelsin
    [SerializeField] private float glowPulse = 1.35f;   // glow biraz daha puls

    float t01;

    void Awake()
    {
        // Root’ta LR yok dedik ama güvenlik: child’lardan otomatik bul
        if (core == null || glow == null)
        {
            var lrs = GetComponentsInChildren<LineRenderer>(true);
            if (lrs != null && lrs.Length > 0)
            {
                if (core == null) core = lrs[0];
                if (glow == null && lrs.Length > 1) glow = lrs[1];
            }
        }

        if (core == null || glow == null)
        {
            Debug.LogError("SpearLineVFX: Core/Glow LineRenderer missing! Prefab wiring broken.", this);
            enabled = false;
            return;
        }

        SetupLine(core, coreWidth, coreColor, isGlow: false);
        SetupLine(glow, glowWidth, glowColor, isGlow: true);
    }


    void SetupLine(LineRenderer lr, float mainWidth, Color baseCol, bool isGlow)
    {
        if (lr == null) return;

        lr.positionCount = 2;
        lr.useWorldSpace = true;

        // Uçlar ince, ortası kalın (taper)
        var curve = new AnimationCurve(
            new Keyframe(0f, tipWidth),
            new Keyframe(0.12f, mainWidth),
            new Keyframe(0.88f, mainWidth),
            new Keyframe(1f, tipWidth)
        );
        lr.widthCurve = curve;

        // Alpha/Color gradient: başta parlak, sonra sön
        // Core daha keskin, glow daha yumuşak
        Gradient g = new Gradient();

        // Renk (core aynı, glow aynı) — asıl iş alpha’da
        g.colorKeys = new[]
        {
            new GradientColorKey(new Color(baseCol.r, baseCol.g, baseCol.b, 1f), 0f),
            new GradientColorKey(new Color(baseCol.r, baseCol.g, baseCol.b, 1f), 1f)
        };

        float a0 = baseCol.a;
        g.alphaKeys = isGlow
            ? new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(a0, 0.10f),
                new GradientAlphaKey(a0 * 0.8f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            }
            : new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(a0, 0.06f),
                new GradientAlphaKey(a0, 0.35f),
                new GradientAlphaKey(0f, 1f)
            };

        lr.colorGradient = g;
        // ✅ LineRenderer'ın eski color/gradient ayarlarıyla çakışmasın
        lr.startColor = Color.white;
        lr.endColor = Color.white;
        lr.enabled = true;


        // LineRenderer’ın kendi widthMultiplier’ı animasyonda kullanılacak
        lr.widthMultiplier = 1f;
    }

    public void Play(Vector2 start, Vector2 end)
    {
        if (core == null || glow == null)
        {
            Debug.LogError("SpearLineVFX.Play called but LineRenderers are missing!", this);
            return;
        }

        if (core != null)
        {
            core.SetPosition(0, start);
            core.SetPosition(1, end);
        }
        if (glow != null)
        {
            glow.SetPosition(0, start);
            glow.SetPosition(1, end);
        }

        t01 = 0f;

        // ilk frame şok etkisi
        if (core != null) core.widthMultiplier = startGrow;
        if (glow != null) glow.widthMultiplier = startGrow * glowPulse;
    }

    void Update()
    {
        if (core == null || glow == null) return;

        t01 += Time.deltaTime / Mathf.Max(0.001f, lifetime);
        float t = Mathf.Clamp01(t01);

        // Kısa ve net “flash” eğrisi (0..1)
        // Başta hızlı büyüme, sonra sönüş
        float flash = (t < 0.25f)
            ? Mathf.Lerp(startGrow, 1f, t / 0.25f)
            : Mathf.Lerp(1f, endShrink, (t - 0.25f) / 0.75f);

        if (core != null) core.widthMultiplier = flash;
        if (glow != null) glow.widthMultiplier = flash * glowPulse;

        // İstersen çizgi “uç parlaması” hissi için küçük bir jitter:
        // (çok az ver, yoksa titreme gibi durur)
        // float j = (1f - t) * 0.02f;
        // if (glow != null) glow.transform.position = new Vector3(Random.Range(-j,j), Random.Range(-j,j), 0f);

        if (t >= 1f) Destroy(gameObject);
    }
}
