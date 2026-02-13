using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverGlowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image targetImage;
    [SerializeField] private float brightnessMultiplier = 1.15f;
    [SerializeField] private float lerpSpeed = 10f;

    private Color baseColor;
    private Color targetColor;

    void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        baseColor = targetImage.color;
        targetColor = baseColor;
    }

    void Update()
    {
        targetImage.color = Color.Lerp(
            targetImage.color,
            targetColor,
            Time.unscaledDeltaTime * lerpSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetColor = baseColor * brightnessMultiplier;
        targetColor.a = baseColor.a; // alpha bozulmasın
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetColor = baseColor;
    }
}
