using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradePanel : MonoBehaviour
{
    private struct UpgradeChoice
    {
        public UpgradeType type;
        public int value;
    }

    private enum UpgradeType
    {
        Strength,
        AttackSpeed,
        MaxHealth,
        MoveSpeed
    }

    private UpgradeChoice[] choices = new UpgradeChoice[3];
    private bool[] rerollUsed = new bool[3];

    public GameManager gameManager;

    [Header("Augment Buttons + Texts")]
    [SerializeField] Button firstAugmentButton;
    [SerializeField] TMP_Text firstAugmentText;
    [SerializeField] Button secondAugmentButton;
    [SerializeField] TMP_Text secondAugmentText;
    [SerializeField] Button thirdAugmentButton;
    [SerializeField] TMP_Text thirdAugmentText;

    [Header("Reroll Buttons")]
    [SerializeField] Button firstRerollButton;
    [SerializeField] Button secondRerollButton;
    [SerializeField] Button thirdRerollButton;

    private Player player;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        player = FindFirstObjectByType<Player>();
    }

    private void Start()
    {
        // Augment seçimi (0-1-2)
        firstAugmentButton.onClick.AddListener(() => OnAugmentSelected(0));
        secondAugmentButton.onClick.AddListener(() => OnAugmentSelected(1));
        thirdAugmentButton.onClick.AddListener(() => OnAugmentSelected(2));

        // Reroll (0-1-2)
        firstRerollButton.onClick.AddListener(() => OnRerollSelected(0));
        secondRerollButton.onClick.AddListener(() => OnRerollSelected(1));
        thirdRerollButton.onClick.AddListener(() => OnRerollSelected(2));
    }

    public void Open()
    {
        gameObject.SetActive(true);

        // player sahnede spawn oluyorsa Awake'te bulamamış olabilir
        if (player == null)
            player = FindFirstObjectByType<Player>();

        // reroll haklarını sıfırla
        for (int i = 0; i < 3; i++)
            rerollUsed[i] = false;

        firstRerollButton.interactable = true;
        secondRerollButton.interactable = true;
        thirdRerollButton.interactable = true;

        // 3 kartı doldur
        RollChoiceUnique(0, false);
        RollChoiceUnique(1, false);
        RollChoiceUnique(2, false);


        RefreshTexts();
    }

    private string GetKey(UpgradeChoice c)
    {
        return $"{c.type}_{c.value}";
    }

    private void RollChoiceUnique(int index, bool isReroll)
    {
        // 1) Yasakları topla
        // Diğer kartlarda görünen seçimler yasak
        var banned = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 3; i++)
        {
            if (i == index) continue;

            // Eğer o slot daha önce roll edildiyse (value > 0) ekle
            if (choices[i].value > 0)
                banned.Add(GetKey(choices[i]));
        }

        // Reroll ise kendi eski seçeneğini de yasakla
        string oldKey = null;
        if (isReroll && choices[index].value > 0)
        {
            oldKey = GetKey(choices[index]);
            banned.Add(oldKey);
        }

        // 2) Deneyerek bul (sonsuz döngü yok)
        const int maxTries = 20;
        UpgradeChoice candidate = default;

        for (int t = 0; t < maxTries; t++)
        {
            candidate = GenerateRandomChoice();   // birazdan yazacağız
            var key = GetKey(candidate);

            if (!banned.Contains(key))
            {
                choices[index] = candidate;
                return;
            }
        }

        // Bulamadıysa: en son denemeyi bas (çok nadir olur)
        choices[index] = candidate;
    }

    private UpgradeChoice GenerateRandomChoice()
    {
        UpgradeType[] possibleTypes =
        {
        UpgradeType.Strength,
        UpgradeType.MoveSpeed,
        UpgradeType.AttackSpeed,
        UpgradeType.MaxHealth
    };

        UpgradeType type = possibleTypes[Random.Range(0, possibleTypes.Length)];

        int value;
        switch (type)
        {
            case UpgradeType.Strength:
                value = Random.Range(1, 3); // 1-2
                break;

            case UpgradeType.MoveSpeed:
                value = Random.Range(1, 3); // 1-2
                break;

            case UpgradeType.AttackSpeed:
                value = Random.Range(1, 3); // 1-2
                break;

            case UpgradeType.MaxHealth:
                value = (Random.value < 0.5f) ? 2 : 4;
                break;

            default:
                value = 1;
                break;
        }

        return new UpgradeChoice { type = type, value = value };
    }

    private void RefreshTexts()
    {
        firstAugmentText.text = ChoiceToString(choices[0]);
        secondAugmentText.text = ChoiceToString(choices[1]);
        thirdAugmentText.text = ChoiceToString(choices[2]);
    }

    private string ChoiceToString(UpgradeChoice c)
    {
        switch (c.type)
        {
            case UpgradeType.Strength: return $"Strength +{c.value}";
            case UpgradeType.MoveSpeed: return $"Move Speed +{c.value}";
            case UpgradeType.AttackSpeed: return $"Attack Speed +{c.value}%";
            case UpgradeType.MaxHealth: return $"Max Health +{c.value}";
            default: return "-";
        }
    }

    private void OnAugmentSelected(int index)
    {
        ApplyChoiceToPlayer(choices[index]);

        // panel kapan + oyun devam
        gameObject.SetActive(false);
        gameManager.OnUpgradeChosen();

    }

    private void ApplyChoiceToPlayer(UpgradeChoice c)
    {
        if (player == null) return;

        switch (c.type)
        {
            case UpgradeType.Strength:
                player.ChangeStrength(c.value);
                break;

            case UpgradeType.MoveSpeed:
                player.ChangeMoveSpeed(c.value);
                break;

            case UpgradeType.AttackSpeed:
                player.ChangeAttackSpeedPercent(c.value);
                break;

            case UpgradeType.MaxHealth:
                player.ChangeMaxHealth(c.value);
                break;
        }
    }

    private void OnRerollSelected(int index)
    {
        if (rerollUsed[index]) return;

        RollChoiceUnique(index, true);
        rerollUsed[index] = true;

        if (index == 0) firstRerollButton.interactable = false;
        if (index == 1) secondRerollButton.interactable = false;
        if (index == 2) thirdRerollButton.interactable = false;

        RefreshTexts();
    }
}
