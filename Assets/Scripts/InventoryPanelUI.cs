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

    [Header("Weapon Slot UI (Fixed)")]
    [SerializeField] private InventoryWeaponSlotUI firstWeapon;
    [SerializeField] private InventoryWeaponSlotUI secondWeapon;
    [SerializeField] private InventoryWeaponSlotUI thirdWeapon;
    [SerializeField] private InventoryWeaponSlotUI fourthWeapon;

    [Header("Weapon Icons")]
    [SerializeField] private Sprite rifleSprite;
    [SerializeField] private Sprite shotgunSprite;
    [SerializeField] private Sprite sniperSprite;
    [SerializeField] private Sprite swordSprite;
    [SerializeField] private Sprite spearSprite;
    [SerializeField] private Sprite hammerSprite;

    private InventoryWeaponSlotUI[] weaponUI;

    [Header("Prefabs")]
    [SerializeField] private InventoryEntryUI entryPrefab;

    private bool isOpen;

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        weaponUI = new[] { firstWeapon, secondWeapon, thirdWeapon, fourthWeapon };
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



        // ✅ Item listesi dinamik olduğu için temizlenir
        ClearChildren(itemsParent);

        // ---- WEAPONS (Fixed UI Slots) ----
        for (int i = 0; i < weaponUI.Length; i++)
        {
            if (weaponUI[i] == null) continue;

            if (player.weaponSlots == null || i >= player.weaponSlots.Count || player.weaponSlots[i] == null)
            {
                weaponUI[i].SetEmpty();
                continue;
            }

            var ws = player.weaponSlots[i];

            int need = Player.NeededCopiesForNext(ws.level);
            string prog = (need > 0) ? $" ({ws.copiesTowardNext}/{need})" : " (MAX)";
            string label = $"{ws.type} Lv{ws.level}{prog}";

            if (!ws.isActive || ws.type == Player.WeaponType.None)
            {
                weaponUI[i].SetEmpty();
                continue;
            }

            Sprite icon = GetWeaponIcon(ws.type);
            Debug.Log($"[InvWeapon] slot{i} type={ws.type} iconNull={(GetWeaponIcon(ws.type) == null)} active={ws.isActive}");


            weaponUI[i].SetWeapon(
                label,
                ws.type.ToString(),
                icon,
                ws.damage,
                ws.interval,
                ws.baseRange
            );
        }

        // ---- ITEMS (Scroll Content) ----
        foreach (var it in player.ownedItems)
        {
            var e = Instantiate(entryPrefab, itemsParent);
            e.Set(it.icon, it.title, it.desc, $"Cost:{it.cost}", it.rarity);
        }
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    private Sprite GetWeaponIcon(Player.WeaponType type)
    {
        return type switch
        {
            Player.WeaponType.Rifle => rifleSprite,
            Player.WeaponType.Shotgun => shotgunSprite,
            Player.WeaponType.Sniper => sniperSprite,
            Player.WeaponType.Sword => swordSprite,
            Player.WeaponType.Spear => spearSprite,
            Player.WeaponType.Hammer => hammerSprite,
            _ => null
        };
    }
}
