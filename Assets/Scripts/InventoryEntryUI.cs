using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryEntryUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text subText;

    [SerializeField] private Image background;

    [Header("Rarity Colors (same as Shop)")]
    [SerializeField] private Color commonBg = Color.white;
    [SerializeField] private Color uncommonColor = new Color(0.45f, 1f, 0.55f);
    [SerializeField] private Color rareBg = new Color(0.45f, 0.7f, 1f);
    [SerializeField] private Color epicBg = new Color(0.85f, 0.45f, 1f);
    [SerializeField] private Color legendaryBg = new Color(1f, 0.75f, 0.25f);


    public void Set(Sprite icon, string title, string desc, string sub, int rarity)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        var tt = GetComponent<TooltipTrigger>();
        if (tt != null)
        {
            tt.title = title;
            tt.body = desc + "\n" + sub;   // istersen daha güzel formatlarız
            tt.icon = icon;
        }

        if (background != null)
            background.color = GetRarityColor(rarity);
    }

    private Color GetRarityColor(int rarity)
    {
        // 0: Common, 1: Rare, 2: Epic, 3: Legendary (senin değerlerin farklıysa ayarlarız)
        return rarity switch
        {
            0 => commonBg,
            1 => uncommonColor,
            2 => rareBg,
            3 => epicBg,
            4 => legendaryBg,
            _ => commonBg
        };
    }
}
