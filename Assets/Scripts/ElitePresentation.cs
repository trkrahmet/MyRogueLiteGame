using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ElitePresentation : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameManager gm;
    [SerializeField] private EnemySpawner spawner;

    [Header("Arena Visual Root (sadece görsel parent)")]
    [SerializeField] private Transform arenaVisualRoot;
    [SerializeField] private Vector3 combatScale = Vector3.one;
    [SerializeField] private Vector3 eliteScale = new Vector3(0.78f, 0.78f, 1f);
    [SerializeField] private float arenaAnimDuration = 0.6f;
    [SerializeField] private AnimationCurve arenaCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Screen Overlay (kararma)")]
    [SerializeField] private Image overlay;
    [SerializeField] private float eliteOverlayAlpha = 0.55f;
    [SerializeField] private float overlayDuration = 0.35f;

    [Header("Camera FX")]
    [SerializeField] private Camera cam;
    [SerializeField] private float combatOrthoSize = 6.5f;
    [SerializeField] private float eliteOrthoSize = 5.2f;
    [SerializeField] private float camZoomDuration = 0.45f;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeStrength = 0.12f;

    [Header("Boss UI")]
    [SerializeField] private BossHpUI bossHpUI; // aşağıda yazacağım

    Coroutine arenaRoutine, overlayRoutine, camRoutine, shakeRoutine;

    private Vector3 camBasePos;

    private void Awake()
    {
        if (gm == null) gm = FindFirstObjectByType<GameManager>();
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (cam == null) cam = Camera.main;
        if (cam != null) camBasePos = cam.transform.position;
    }

    private void OnEnable()
    {
        if (gm != null)
        {
            gm.OnEliteStarted += OnEliteStart;
            gm.OnEliteEnded += OnEliteEnd;
        }

        if (spawner != null)
            spawner.OnFinalEnemySpawned += OnFinalEnemySpawned;
    }

    private void OnDisable()
    {
        if (gm != null)
        {
            gm.OnEliteStarted -= OnEliteStart;
            gm.OnEliteEnded -= OnEliteEnd;
        }

        if (spawner != null)
            spawner.OnFinalEnemySpawned -= OnFinalEnemySpawned;
    }

    void OnEliteStart()
    {
        // arena shrink + kararma + zoom + küçük shake (giriş vurucu olsun)
        StartArenaScale(eliteScale);
        StartOverlay(eliteOverlayAlpha);
        StartCameraZoom(eliteOrthoSize);
        StartShake(shakeDuration, shakeStrength);
    }

    void OnEliteEnd()
    {
        // combat'a geri
        StartArenaScale(combatScale);
        StartOverlay(0f);
        StartCameraZoom(combatOrthoSize);

        if (bossHpUI != null) bossHpUI.Hide();
    }

    void OnFinalEnemySpawned(Enemy e)
    {
        if (bossHpUI == null || e == null) return;

        string bossName = e.gameObject.name;

        // Unity "(Clone)" ekler, onu temizleyelim
        bossName = bossName.Replace("(Clone)", "").Trim();

        bossHpUI.Show(e, bossName);
    }


    void StartArenaScale(Vector3 target)
    {
        if (arenaVisualRoot == null) return;
        if (arenaRoutine != null) StopCoroutine(arenaRoutine);
        arenaRoutine = StartCoroutine(ScaleRoutine(target));
    }

    IEnumerator ScaleRoutine(Vector3 target)
    {
        Vector3 start = arenaVisualRoot.localScale;
        float t = 0f;

        while (t < arenaAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = arenaCurve.Evaluate(Mathf.Clamp01(t / arenaAnimDuration));
            arenaVisualRoot.localScale = Vector3.LerpUnclamped(start, target, a);
            yield return null;
        }

        arenaVisualRoot.localScale = target;
        arenaRoutine = null;
    }

    void StartOverlay(float targetA)
    {
        if (overlay == null) return;
        if (overlayRoutine != null) StopCoroutine(overlayRoutine);
        overlayRoutine = StartCoroutine(OverlayRoutine(targetA));
    }

    IEnumerator OverlayRoutine(float targetA)
    {
        Color c = overlay.color;
        float startA = c.a;
        float t = 0f;

        while (t < overlayDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / overlayDuration);
            c.a = Mathf.Lerp(startA, targetA, a);
            overlay.color = c;
            yield return null;
        }

        c.a = targetA;
        overlay.color = c;
        overlayRoutine = null;
    }

    void StartCameraZoom(float targetSize)
    {
        if (cam == null || !cam.orthographic) return;
        if (camRoutine != null) StopCoroutine(camRoutine);
        camRoutine = StartCoroutine(CameraZoomRoutine(targetSize));
    }

    IEnumerator CameraZoomRoutine(float targetSize)
    {
        float start = cam.orthographicSize;
        float t = 0f;

        while (t < camZoomDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / camZoomDuration);
            cam.orthographicSize = Mathf.Lerp(start, targetSize, a);
            yield return null;
        }

        cam.orthographicSize = targetSize;
        camRoutine = null;
    }

    void StartShake(float dur, float str)
    {
        if (cam == null) return;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(dur, str));
    }

    IEnumerator ShakeRoutine(float dur, float str)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            Vector2 r = Random.insideUnitCircle * str;
            cam.transform.position = camBasePos + new Vector3(r.x, r.y, 0f);
            yield return null;
        }
        cam.transform.position = camBasePos;
        shakeRoutine = null;
    }
}
