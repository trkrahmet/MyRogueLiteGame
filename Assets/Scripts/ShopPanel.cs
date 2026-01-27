using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour
{
    // -------------------- DATA --------------------

    private enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    private enum OfferType
    {
        StatItem,
        Weapon
    }

    private enum StatType
    {
        Strength,
        MaxHealth,
        MoveSpeed,
        AttackSpeedPercent,
        Armor,          // ✅
        HpRegen,        // ✅ (floatDelta)
        PickupRange,     // ✅ (floatDelta)
        CriticalChance,
        Luck
    }

    [Serializable]
    private struct StatDelta
    {
        public StatType stat;

        // Strength, MaxHealth, AttackSpeedPercent için bunu kullan
        public int intDelta;

        // MoveSpeed için bunu kullan
        public float floatDelta;
    }

    [Serializable]
    private class StatItemDefinition
    {
        public Rarity rarity = Rarity.Common;
        public int weight = 100; // aynı rarity içindeki itemlar arasında seçim ağırlığı
        public string title;
        [TextArea(1, 2)] public string desc;
        public int cost = 10;
        public StatDelta[] deltas;

        public Sprite icon; // İstersen ikon ekleyebilirsin
    }


    private struct Offer
    {
        public OfferType type;

        // Stat item
        public int statItemIndex;

        // Weapon
        public Player.WeaponType weaponType;
        public int cost;
    }

    // -------------------- REFS --------------------

    [Header("Refs")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Player player;

    [Header("Stat Item Pool (Inspector’dan doldur)")]
    [SerializeField] private StatItemDefinition[] statItemPool;

    // -------------------- UI --------------------

    [Header("UI")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Image itemImage0;
    [SerializeField] private Image itemImage1;
    [SerializeField] private Image itemImage2;
    [SerializeField] private Image itemImage3;

    [Header("Weapon Icons")]
    [SerializeField] private Sprite rifleIcon;
    [SerializeField] private Sprite shotgunIcon;
    [SerializeField] private Sprite sniperIcon;
    [SerializeField] private Sprite swordIcon;
    [SerializeField] private Sprite spearIcon;
    [SerializeField] private Sprite hammerIcon;
    [SerializeField] private Sprite defaultIcon;

    [SerializeField] private TMP_Text itemText0;
    [SerializeField] private TMP_Text itemText1;
    [SerializeField] private TMP_Text itemText2;
    [SerializeField] private TMP_Text itemText3;

    [SerializeField] private Button buyButton0;
    [SerializeField] private Button buyButton1;
    [SerializeField] private Button buyButton2;
    [SerializeField] private Button buyButton3;

    [Header("Refresh")]
    [SerializeField] private int refreshCost = 5;
    [SerializeField] private TMP_Text refreshCostText;


    private Offer[] offers = new Offer[4];

    private void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (player == null) player = FindFirstObjectByType<Player>();
    }

    private void Start()
    {
        if (buyButton0 != null) buyButton0.onClick.AddListener(() => TryBuy(0));
        if (buyButton1 != null) buyButton1.onClick.AddListener(() => TryBuy(1));
        if (buyButton2 != null) buyButton2.onClick.AddListener(() => TryBuy(2));
        if (buyButton3 != null) buyButton3.onClick.AddListener(() => TryBuy(3));
    }

    // -------------------- OPEN / CLOSE --------------------

    public void OpenShop()
    {
        gameObject.SetActive(true);

        RollAllOffers();
        RefreshUI();

        if (refreshCostText != null)
            refreshCostText.text = $"Refresh ({refreshCost})";
    }


    public void CloseShop()
    {
        gameObject.SetActive(false);
    }

    // Tik butonu buraya bağlanacak
    public void OnContinueButton()
    {
        CloseShop();
        if (gameManager != null)
            gameManager.ContinueForNextLevel();
    }

    // Refresh butonu buraya bağlanabilir
    public void OnRefreshButton()
    {
        if (player == null) return;

        // para yetmiyorsa refresh yapma
        if (!player.TrySpendGold(refreshCost))
        {
            Debug.Log("Not enough gold to refresh!");
            RefreshUI();
            return;
        }

        RollAllOffers();   // hepsini yenile
        RefreshUI();
    }


    // -------------------- ROLL --------------------

    private void RollAllOffers()
    {
        for (int i = 0; i < offers.Length; i++)
            offers[i] = RollOneOffer();
    }

    private Offer RollOneOffer()
    {
        // Şimdilik: çoğunluk StatItem, bazen Weapon
        // Oran istiyorsan burayı değiştir:
        // 0.80 stat item, 0.20 weapon
        bool wantWeapon = (UnityEngine.Random.value < 0.20f);

        if (wantWeapon)
            return RollWeaponOffer();

        return RollStatItemOffer();
    }

    private Offer RollStatItemOffer()
    {
        Offer o = new Offer();
        o.type = OfferType.StatItem;

        if (statItemPool == null || statItemPool.Length == 0)
        {
            o.statItemIndex = -1;
            return o;
        }

        // Luck’a göre rarity seç
        Rarity r = RollRarityWithLuck();

        // O rarity’den item seç; yoksa bir alt rarity’ye düş
        int idx = PickStatItemIndexByRarity(r);
        if (idx < 0 && r != Rarity.Common)
            idx = PickStatItemIndexByRarity(r - 1); // Uncommon->Common gibi

        if (idx < 0) idx = UnityEngine.Random.Range(0, statItemPool.Length);

        o.statItemIndex = idx;
        return o;
    }


    private Offer RollWeaponOffer()
    {
        Offer o = new Offer();
        o.type = OfferType.Weapon;

        o.weaponType = RollWeaponType();
        o.cost = WeaponCost(o.weaponType);

        return o;
    }

    private Player.WeaponType RollWeaponType()
    {
        // burada hangi silahların shop’ta çıkacağını belirliyorsun
        Player.WeaponType[] pool =
        {
        Player.WeaponType.Rifle,
        Player.WeaponType.Shotgun,

        Player.WeaponType.Sniper,
        Player.WeaponType.Sword,
        Player.WeaponType.Spear,
        Player.WeaponType.Hammer
    };

        int r = UnityEngine.Random.Range(0, pool.Length);
        return pool[r];
    }


    private int WeaponCost(Player.WeaponType wt)
    {
        switch (wt)
        {
            case Player.WeaponType.Rifle: return 25;
            case Player.WeaponType.Shotgun: return 30;

            case Player.WeaponType.Sniper: return 40;

            case Player.WeaponType.Sword: return 28;
            case Player.WeaponType.Spear: return 32;
            case Player.WeaponType.Hammer: return 36;

            default: return 30;
        }
    }


    // -------------------- BUY --------------------

    private void TryBuy(int index)
    {
        if (player == null) return;

        Offer offer = offers[index];

        // Stat item ise cost’u definition’dan alacağız
        int cost = GetOfferCost(offer);
        if (cost <= 0)
        {
            Debug.LogWarning("Offer cost invalid / missing.");
            return;
        }

        if (!player.TrySpendGold(cost))
        {
            Debug.Log("Not enough gold!");
            RefreshUI();
            return;
        }

        bool applied = ApplyOffer(offer);
        if (!applied)
        {
            // başarısızsa parayı iade
            player.AddGold(cost);
            RefreshUI();
            return;
        }

        // Satın alınan satırı yenile
        offers[index] = RollOneOffer();
        RefreshUI();
    }

    private int GetOfferCost(Offer offer)
    {
        if (offer.type == OfferType.Weapon)
            return offer.cost;

        // StatItem
        if (offer.statItemIndex < 0) return 10;
        if (statItemPool == null || offer.statItemIndex >= statItemPool.Length) return 10;
        return Mathf.Max(1, statItemPool[offer.statItemIndex].cost);
    }

    private bool ApplyOffer(Offer offer)
    {
        switch (offer.type)
        {
            case OfferType.StatItem:
                return ApplyStatItem(offer.statItemIndex);

            case OfferType.Weapon:
                bool added = player.TryAddWeapon(offer.weaponType);
                if (!added)
                {
                    Debug.Log("No empty weapon slot!");
                    return false;
                }
                return true;

            default:
                return false;
        }
    }

    private bool ApplyStatItem(int statItemIndex)
    {
        if (statItemPool == null || statItemPool.Length == 0) return false;

        StatItemDefinition def = null;

        if (statItemIndex >= 0 && statItemIndex < statItemPool.Length)
            def = statItemPool[statItemIndex];

        if (def == null)
            return false;

        if (def.deltas == null || def.deltas.Length == 0)
            return false;

        for (int i = 0; i < def.deltas.Length; i++)
        {
            ApplyDelta(def.deltas[i]);
        }

        return true;
    }

    private void ApplyDelta(StatDelta d)
    {
        switch (d.stat)
        {
            case StatType.Luck:
                player.ChangeLuck(d.floatDelta);
                break;

            case StatType.CriticalChance:
                player.ChangeCritChance(d.floatDelta);
                break;

            case StatType.Strength:
                player.ChangeStrength(d.intDelta);
                break;

            case StatType.MaxHealth:
                player.ChangeMaxHealth(d.intDelta);
                break;

            case StatType.MoveSpeed:
                player.ChangeMoveSpeed(d.floatDelta);
                break;

            case StatType.AttackSpeedPercent:
                player.ChangeAttackSpeedPercent(d.intDelta);
                break;

            case StatType.Armor:
                player.ChangeArmor(d.intDelta);
                break;

            case StatType.HpRegen:
                player.ChangeHpRegen(d.floatDelta);
                break;

            case StatType.PickupRange:
                player.ChangePickupRange(d.floatDelta);
                break;
        }
    }

    // -------------------- UI --------------------

    private void RefreshUI()
    {
        if (coinText != null && player != null)
            coinText.text = $"Coin: {player.gold}";

        if (itemText0 != null) itemText0.text = OfferToString(offers[0]);
        if (itemText1 != null) itemText1.text = OfferToString(offers[1]);
        if (itemText2 != null) itemText2.text = OfferToString(offers[2]);
        if (itemText3 != null) itemText3.text = OfferToString(offers[3]);

        // ✅ iconlar
        if (itemImage0 != null) itemImage0.sprite = OfferToIcon(offers[0]);
        if (itemImage1 != null) itemImage1.sprite = OfferToIcon(offers[1]);
        if (itemImage2 != null) itemImage2.sprite = OfferToIcon(offers[2]);
        if (itemImage3 != null) itemImage3.sprite = OfferToIcon(offers[3]);
    }

    private Sprite OfferToIcon(Offer offer)
    {
        if (offer.type == OfferType.Weapon)
        {
            switch (offer.weaponType)
            {
                case Player.WeaponType.Rifle: return rifleIcon != null ? rifleIcon : defaultIcon;
                case Player.WeaponType.Shotgun: return shotgunIcon != null ? shotgunIcon : defaultIcon;

                case Player.WeaponType.Sniper: return sniperIcon != null ? sniperIcon : defaultIcon;
                case Player.WeaponType.Sword: return swordIcon != null ? swordIcon : defaultIcon;
                case Player.WeaponType.Spear: return spearIcon != null ? spearIcon : defaultIcon;
                case Player.WeaponType.Hammer: return hammerIcon != null ? hammerIcon : defaultIcon;
            }
            return defaultIcon;
        }

        // Stat item
        if (statItemPool == null || statItemPool.Length == 0) return defaultIcon;
        if (offer.statItemIndex < 0 || offer.statItemIndex >= statItemPool.Length) return defaultIcon;

        var def = statItemPool[offer.statItemIndex];
        return def.icon != null ? def.icon : defaultIcon;
    }

    private string OfferToString(Offer offer)
    {
        if (offer.type == OfferType.Weapon)
        {
            return $"Weapon: {offer.weaponType} ({offer.cost})";
        }

        // Stat item
        if (statItemPool == null || statItemPool.Length == 0)
            return "No items (pool empty)";

        if (offer.statItemIndex < 0 || offer.statItemIndex >= statItemPool.Length)
            return "Item (?)";

        var def = statItemPool[offer.statItemIndex];
        // 1 satır: isim + kısa desc + fiyat
        string desc = string.IsNullOrWhiteSpace(def.desc) ? "" : $" - {def.desc}";
        return $"{def.title}{desc} ({def.cost})";
    }

    private Rarity RollRarityWithLuck()
    {
        float L = (player != null) ? player.luck : 0f;

        // Base ağırlıklar (Luck=0)
        float common = 100f;
        float uncom = 45f;
        float rare = 18f;
        float epic = 5f;
        float leg = 1f;

        // Luck etkisi: üst rarityleri artır, common’ı azalt
        // (Değerler safe: legendary yine nadir)
        float t = L / 100f; // 0..1

        common *= Mathf.Lerp(1.00f, 0.55f, t);
        uncom *= Mathf.Lerp(1.00f, 1.35f, t);
        rare *= Mathf.Lerp(1.00f, 1.80f, t);
        epic *= Mathf.Lerp(1.00f, 2.40f, t);
        leg *= Mathf.Lerp(1.00f, 3.00f, t);

        float total = common + uncom + rare + epic + leg;
        float roll = UnityEngine.Random.value * total;

        if ((roll -= common) < 0) return Rarity.Common;
        if ((roll -= uncom) < 0) return Rarity.Uncommon;
        if ((roll -= rare) < 0) return Rarity.Rare;
        if ((roll -= epic) < 0) return Rarity.Epic;
        return Rarity.Legendary;
    }

    private int PickStatItemIndexByRarity(Rarity r)
    {
        if (statItemPool == null || statItemPool.Length == 0) return -1;

        int total = 0;
        for (int i = 0; i < statItemPool.Length; i++)
            if (statItemPool[i] != null && statItemPool[i].rarity == r)
                total += Mathf.Max(1, statItemPool[i].weight);

        if (total <= 0) return -1; // o rarity'de item yok

        int roll = UnityEngine.Random.Range(0, total);
        for (int i = 0; i < statItemPool.Length; i++)
        {
            var def = statItemPool[i];
            if (def == null || def.rarity != r) continue;

            roll -= Mathf.Max(1, def.weight);
            if (roll < 0) return i;
        }
        return -1;
    }

}
