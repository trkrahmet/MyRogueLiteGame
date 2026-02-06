using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryWeaponSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statsText;

    public void SetEmpty()
    {
        if (icon != null) { icon.sprite = null; icon.enabled = false; }
        if (nameText != null) nameText.text = "Empty";
        if (statsText != null) statsText.text = "";
    }

    public void SetWeapon(string name, Sprite sprite, float dmg, float interval, float range)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        var tt = GetComponent<TooltipTrigger>();
        if (tt != null)
        {
            tt.title = name;
            tt.body = $"Damage: {dmg}\nAttack Speed: {interval:0.00}\nRange: {range:0.0}";
            tt.icon = sprite;
        }
    }
}
