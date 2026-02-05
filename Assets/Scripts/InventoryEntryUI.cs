using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryEntryUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text subText;

    public void Set(Sprite icon, string title, string desc, string sub)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (titleText != null) titleText.text = title;
        if (descText != null) descText.text = desc;
        if (subText != null) subText.text = sub;
    }
}
