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
        FireRate,
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
        RollChoice(0);
        RollChoice(1);
        RollChoice(2);

        RefreshTexts();
    }

    private void RollChoice(int index)
    {
        // 4 tipten birini random seç
        UpgradeType[] possibleTypes =
        {
            UpgradeType.Strength,
            UpgradeType.MoveSpeed,
            UpgradeType.FireRate,
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

            case UpgradeType.FireRate:
                value = Random.Range(1, 3); // 1-2
                break;

            case UpgradeType.MaxHealth:
                value = (Random.value < 0.5f) ? 2 : 4; // 2 veya 4
                break;

            default:
                value = 1;
                break;
        }

        choices[index] = new UpgradeChoice { type = type, value = value };
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
            case UpgradeType.Strength:  return $"Strength +{c.value}";
            case UpgradeType.MoveSpeed: return $"Move Speed +{c.value}";
            case UpgradeType.FireRate:  return $"Fire Rate +{c.value}";
            case UpgradeType.MaxHealth: return $"Max Health +{c.value}";
            default: return "-";
        }
    }

    private void OnAugmentSelected(int index)
    {
        // Güvenlik: choice hiç roll edilmediyse (value 0) burada roll et
        if (choices[index].value <= 0)
        {
            RollChoice(index);
            RefreshTexts();
        }

        ApplyChoiceToPlayer(choices[index]);

        // panel kapan + oyun devam
        gameObject.SetActive(false);
        gameManager.ContinueAfterUpgrade();
    }

    private void ApplyChoiceToPlayer(UpgradeChoice c)
    {
        if (player == null) return;

        switch (c.type)
        {
            case UpgradeType.Strength:
                player.IncreaseStrength(c.value);
                break;

            case UpgradeType.MoveSpeed:
                player.IncreaseMoveSpeed(c.value);
                break;

            case UpgradeType.FireRate:
                player.IncreaseFireRate(c.value);
                break;

            case UpgradeType.MaxHealth:
                player.IncreaseMaxHealth(c.value);
                break;
        }
    }

    private void OnRerollSelected(int index)
    {
        if (rerollUsed[index]) return;

        RollChoice(index);
        rerollUsed[index] = true;

        if (index == 0) firstRerollButton.interactable = false;
        if (index == 1) secondRerollButton.interactable = false;
        if (index == 2) thirdRerollButton.interactable = false;

        RefreshTexts();
    }
}
