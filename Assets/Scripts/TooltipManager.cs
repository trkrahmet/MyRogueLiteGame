using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager I { get; private set; }

    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image iconImage;

    [SerializeField] private Vector2 offset = new Vector2(10, -12);


    [SerializeField] private float showDelay = 0.15f;
    private Coroutine showRoutine;

    private Canvas rootCanvas;

    private void Awake()
    {
        I = this;

        if (group == null) group = GetComponent<CanvasGroup>();
        if (panel == null) panel = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();

        if (group != null)
        {
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        Hide();
    }

    private void Update()
    {
        if (group == null || group.alpha <= 0f) return;

        // Tooltip mouse'u takip etsin (UI canvas space'e göre)
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            Input.mousePosition,
            rootCanvas.worldCamera,
            out pos
        );

        var canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        Vector2 desired = pos + offset;

        // Panel boyutu
        float w = panel.rect.width;
        float h = panel.rect.height;

        // Panel pivotunu hesaba katarak bounds hesapla
        float left = desired.x - panel.pivot.x * w;
        float right = left + w;
        float bottom = desired.y - panel.pivot.y * h;
        float top = bottom + h;

        // Canvas bounds (local)
        var c = canvasRect.rect;
        float pad = 8f;

        // Taşma varsa içeri it (clamp)
        if (right > c.xMax - pad) desired.x -= (right - (c.xMax - pad));
        if (left < c.xMin + pad) desired.x += ((c.xMin + pad) - left);

        if (top > c.yMax - pad) desired.y -= (top - (c.yMax - pad));
        if (bottom < c.yMin + pad) desired.y += ((c.yMin + pad) - bottom);

        panel.anchoredPosition = desired;

    }

    public void Show(string title, string body, Sprite icon = null)
    {
        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(ShowDelayed(title, body, icon));

        // ✅ tooltip asla raycast bloklamasın
        if (group != null)
        {
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    IEnumerator ShowDelayed(string title, string body, Sprite icon)
    {
        yield return new WaitForSecondsRealtime(showDelay);

        titleText.text = title;
        bodyText.text = body;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        group.alpha = 1f;
    }

    public void Hide()
    {
        if (showRoutine != null) StopCoroutine(showRoutine);

        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}
