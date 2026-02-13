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

    [Header("Weapon Icons (Inventory - Per Level)")]
    [SerializeField] private Sprite rifleSprite;
    [SerializeField] private Sprite rifleLv2;
    [SerializeField] private Sprite rifleLv3;

    [SerializeField] private Sprite shotgunSprite;
    [SerializeField] private Sprite shotgunLv2;
    [SerializeField] private Sprite shotgunLv3;

    [SerializeField] private Sprite sniperSprite;
    [SerializeField] private Sprite sniperLv2;
    [SerializeField] private Sprite sniperLv3;

    [SerializeField] private Sprite swordSprite;
    [SerializeField] private Sprite swordLv2;
    [SerializeField] private Sprite swordLv3;

    [SerializeField] private Sprite spearSprite;
    [SerializeField] private Sprite spearLv2;
    [SerializeField] private Sprite spearLv3;

    [SerializeField] private Sprite hammerSprite;
    [SerializeField] private Sprite hammerLv2;
    [SerializeField] private Sprite hammerLv3;

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

            Sprite icon = GetWeaponIcon(ws.type, ws.level);

            Debug.Log($"[InvWeapon] slot{i} type={ws.type} iconNull={(GetWeaponIcon(ws.type, ws.level) == null)} active={ws.isActive}");


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

    private Sprite GetWeaponIcon(Player.WeaponType type, int level)
    {
        level = Mathf.Clamp(level, 1, 3);

        switch (type)
        {
            case Player.WeaponType.Rifle:
                return level == 3 ? (rifleLv3 != null ? rifleLv3 : rifleSprite)
                     : level == 2 ? (rifleLv2 != null ? rifleLv2 : rifleSprite)
                     : rifleSprite;

            case Player.WeaponType.Shotgun:
                return level == 3 ? (shotgunLv3 != null ? shotgunLv3 : shotgunSprite)
                     : level == 2 ? (shotgunLv2 != null ? shotgunLv2 : shotgunSprite)
                     : shotgunSprite;

            case Player.WeaponType.Sniper:
                return level == 3 ? (sniperLv3 != null ? sniperLv3 : sniperSprite)
                     : level == 2 ? (sniperLv2 != null ? sniperLv2 : sniperSprite)
                     : sniperSprite;

            case Player.WeaponType.Sword:
                return level == 3 ? (swordLv3 != null ? swordLv3 : swordSprite)
                     : level == 2 ? (swordLv2 != null ? swordLv2 : swordSprite)
                     : swordSprite;

            case Player.WeaponType.Spear:
                return level == 3 ? (spearLv3 != null ? spearLv3 : spearSprite)
                     : level == 2 ? (spearLv2 != null ? spearLv2 : spearSprite)
                     : spearSprite;

            case Player.WeaponType.Hammer:
                return level == 3 ? (hammerLv3 != null ? hammerLv3 : hammerSprite)
                     : level == 2 ? (hammerLv2 != null ? hammerLv2 : hammerSprite)
                     : hammerSprite;
        }

        return null;
    }

}
