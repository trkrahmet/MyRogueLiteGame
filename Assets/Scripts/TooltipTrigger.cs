using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea] public string title;
    [TextArea] public string body;
    public Sprite icon;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.I != null)
            TooltipManager.I.Show(title, body, icon);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.I != null)
            TooltipManager.I.Hide();
    }
}
