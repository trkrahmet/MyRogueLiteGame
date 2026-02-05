using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanelUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Player player;

    [Header("CanvasGroup Toggle")]
    [SerializeField] private CanvasGroup group;

    [Header("UI Parents")]
    [SerializeField] private Transform weaponsParent;
    [SerializeField] private Transform itemsParent;

    [Header("Prefabs")]
    [SerializeField] private InventoryEntryUI entryPrefab;

    private bool isOpen;

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        SetOpen(false);
    }

    public void Toggle()
    {
        SetOpen(!isOpen);
        if (isOpen) Refresh();
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (group != null)
        {
            group.alpha = open ? 1f : 0f;
            group.interactable = open;
            group.blocksRaycasts = open;
        }
        else
        {
            gameObject.SetActive(open);
        }
    }

    public void Refresh()
    {
        Debug.Log($"[Inventory] player={(player != null)} itemsParent={(itemsParent != null)} entryPrefab={(entryPrefab != null)} ownedItems={(player != null ? player.ownedItems.Count : -1)}");

        if (player == null) return;

        ClearChildren(weaponsParent);
        ClearChildren(itemsParent);

        // ---- WEAPONS (weaponSlots'tan) ----
        foreach (var ws in player.weaponSlots)
        {
            if (ws == null) continue;
            if (ws.type == Player.WeaponType.None) continue;
            if (!ws.isActive) continue;

            var e = Instantiate(entryPrefab, weaponsParent);
            e.Set(
                icon: null, // istersen burada ikon bağlarız (aşağıda anlatıyorum)
                title: ws.type.ToString(),
                desc: $"DMG:{ws.damage}  INT:{ws.interval:0.00}  RNG:{ws.baseRange:0.0}",
                sub: "Weapon"
            );
        }

        // ---- ITEMS (ownedItems'tan) ----
        foreach (var it in player.ownedItems)
        {
            var e = Instantiate(entryPrefab, itemsParent);
            e.Set(
                icon: it.icon,
                title: it.title,
                desc: it.desc,
                sub: $"Cost:{it.cost}  Rarity:{it.rarity}"
            );
        }
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }
}
