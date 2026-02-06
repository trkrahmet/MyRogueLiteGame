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

        float width = panel.rect.width;
        Vector2 finalOffset = offset;

        // Eğer mouse ekranın sağ tarafındaysa tooltip sola geçsin
        if (Input.mousePosition.x > Screen.width * 0.8f)
        {
            finalOffset.x = -width - 8f;
        }

        panel.anchoredPosition = pos + finalOffset;

    }

    public void Show(string title, string body, Sprite icon = null)
    {
        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(ShowDelayed(title, body, icon));
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
        group.alpha = 0f;
    }
}
