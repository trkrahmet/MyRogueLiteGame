using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour
{
    private enum ShopItemType
    {
        Strength,
        MaxHealth,
        MoveSpeed,
        AttackSpeed,
        Weapon
    }

    private struct ShopItem
    {
        public ShopItemType type;
        public int value;                 // stat için (Strength +1, MaxHealth +2, AttackSpeed %5 vb.)
        public int cost;                  // fiyat
        public Player.WeaponType weaponType; // sadece Weapon için
    }

    [Header("Refs")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Player player;

    [Header("UI")]
    [SerializeField] private TMP_Text coinText;

    [SerializeField] private TMP_Text itemText0;
    [SerializeField] private TMP_Text itemText1;
    [SerializeField] private TMP_Text itemText2;
    [SerializeField] private TMP_Text itemText3;

    [SerializeField] private Button buyButton0;
    [SerializeField] private Button buyButton1;
    [SerializeField] private Button buyButton2;
    [SerializeField] private Button buyButton3;

    private ShopItem[] items = new ShopItem[4];

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

    public void OpenShop()
    {
        gameObject.SetActive(true);

        RollAllItems();
        RefreshUI();
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

    // (Opsiyonel) refresh butonu buraya bağlanabilir
    public void OnRefreshButton()
    {
        RollAllItems();
        RefreshUI();
    }

    // -------------------- SHOP CORE --------------------

    private void RollAllItems()
    {
        for (int i = 0; i < items.Length; i++)
            items[i] = RollOneItem();
    }

    private ShopItem RollOneItem()
    {
        // Basit havuz:
        // 0-3 stat, 4 weapon
        // Weapon gelme oranını istersen sonra düşürürüz.
        ShopItemType type = (ShopItemType)Random.Range(0, 5);

        ShopItem item = new ShopItem();
        item.type = type;

        switch (type)
        {
            case ShopItemType.Strength:
                item.value = 1;
                item.cost = 10;
                break;

            case ShopItemType.MaxHealth:
                item.value = 2;
                item.cost = 12;
                break;

            case ShopItemType.MoveSpeed:
                item.value = 1;
                item.cost = 10;
                break;

            case ShopItemType.AttackSpeed:
                item.value = 5;   // %5
                item.cost = 15;
                break;

            case ShopItemType.Weapon:
                item.weaponType = RollWeaponType();
                item.value = 0;
                item.cost = WeaponCost(item.weaponType);
                break;
        }

        return item;
    }

    private Player.WeaponType RollWeaponType()
    {
        // Şimdilik Rifle + Shotgun
        // İleride enum'a yeni silah ekleyince otomatik genişletmek istersen farklı yazarız.
        int r = Random.Range(0, 2);
        return (r == 0) ? Player.WeaponType.Rifle : Player.WeaponType.Shotgun;
    }

    private int WeaponCost(Player.WeaponType wt)
    {
        switch (wt)
        {
            case Player.WeaponType.Rifle: return 25;
            case Player.WeaponType.Shotgun: return 30;
            default: return 30;
        }
    }

    private void TryBuy(int index)
    {
        if (player == null) return;

        ShopItem item = items[index];

        // Para yetmiyorsa
        if (!player.TrySpendGold(item.cost))
        {
            Debug.Log("Not enough gold!");
            RefreshUI();
            return;
        }

        // Uygula (başarısız olursa parayı geri ver)
        bool applied = ApplyItem(item);
        if (!applied)
        {
            player.AddGold(item.cost);
            RefreshUI();
            return;
        }

        // Satın alınan satırı yenile (Brotato hissi)
        items[index] = RollOneItem();

        RefreshUI();
    }

    private bool ApplyItem(ShopItem item)
    {
        switch (item.type)
        {
            case ShopItemType.Strength:
                player.IncreaseStrength(item.value);
                return true;

            case ShopItemType.MaxHealth:
                player.IncreaseMaxHealth(item.value);
                return true;

            case ShopItemType.MoveSpeed:
                player.IncreaseMoveSpeed(item.value);
                return true;

            case ShopItemType.AttackSpeed:
                player.IncreaseAttackSpeed(item.value); // %5 gibi
                return true;

            case ShopItemType.Weapon:
                // Boş slot yoksa satın alma başarısız
                bool added = player.TryAddWeapon(item.weaponType);
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

    private void RefreshUI()
    {
        if (coinText != null && player != null)
            coinText.text = $"Coin: {player.gold}";

        if (itemText0 != null) itemText0.text = ItemToString(items[0]);
        if (itemText1 != null) itemText1.text = ItemToString(items[1]);
        if (itemText2 != null) itemText2.text = ItemToString(items[2]);
        if (itemText3 != null) itemText3.text = ItemToString(items[3]);

        // İstersen paran yetmeyenleri buton disable yapabiliriz (sonra)
    }

    private string ItemToString(ShopItem item)
    {
        switch (item.type)
        {
            case ShopItemType.Strength:
                return $"Strength +{item.value} ({item.cost})";

            case ShopItemType.MaxHealth:
                return $"Max Health +{item.value} ({item.cost})";

            case ShopItemType.MoveSpeed:
                return $"Move Speed +{item.value} ({item.cost})";

            case ShopItemType.AttackSpeed:
                return $"Attack Speed +%{item.value} ({item.cost})";

            case ShopItemType.Weapon:
                return $"Weapon: {item.weaponType} ({item.cost})";

            default:
                return "-";
        }
    }
}
