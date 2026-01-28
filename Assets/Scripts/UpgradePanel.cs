using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradePanel : MonoBehaviour
{
    private enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    private struct UpgradeChoice
    {
        public UpgradeType type;
        public int value;      // int üzerinden map yapacağız
        public Rarity rarity;  // ✅
    }

    private enum UpgradeType
    {
        Strength,
        AttackSpeed,     // % (int)
        MaxHealth,
        MoveSpeed,       // value * 0.01f
        Armor,
        HpRegen,         // value * 0.2f /s
        PickupRange,     // value * 1.0f
        CriticalChance,  // % (int)
        Luck,             // value (int -> float)
        Range
    }

    private UpgradeChoice[] choices = new UpgradeChoice[3];
    private bool[] rerollUsed = new bool[3];

    public GameManager gameManager;

    [Header("Card Background Images (Augment1/2/3 üzerindeki Image)")]
    [SerializeField] private Image augmentBg1;
    [SerializeField] private Image augmentBg2;
    [SerializeField] private Image augmentBg3;

    [Header("Card Flash Overlays")]
    [SerializeField] private Image flash1;
    [SerializeField] private Image flash2;
    [SerializeField] private Image flash3;
    [SerializeField] private float uiAnimSpeed = 1f;


    [Header("Rarity Background Sprites")]
    [SerializeField] private Sprite commonBg;
    [SerializeField] private Sprite uncommonBg;
    [SerializeField] private Sprite rareBg;
    [SerializeField] private Sprite epicBg;
    [SerializeField] private Sprite legendaryBg;


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

    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color uncommonColor = new Color(0.45f, 1f, 0.55f);   // yeşilimsi
    [SerializeField] private Color rareColor = new Color(0.45f, 0.7f, 1f);        // mavi
    [SerializeField] private Color epicColor = new Color(0.85f, 0.45f, 1f);       // mor
    [SerializeField] private Color legendaryColor = new Color(1f, 0.75f, 0.25f);  // altın


    private Player player;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        player = FindFirstObjectByType<Player>();
    }

    private void Start()
    {
        // Augment seçimi (0-1-2)
        if (firstAugmentButton != null) firstAugmentButton.onClick.AddListener(() => OnAugmentSelected(0));
        if (secondAugmentButton != null) secondAugmentButton.onClick.AddListener(() => OnAugmentSelected(1));
        if (thirdAugmentButton != null) thirdAugmentButton.onClick.AddListener(() => OnAugmentSelected(2));

        // Reroll (0-1-2)
        if (firstRerollButton != null) firstRerollButton.onClick.AddListener(() => OnRerollSelected(0));
        if (secondRerollButton != null) secondRerollButton.onClick.AddListener(() => OnRerollSelected(1));
        if (thirdRerollButton != null) thirdRerollButton.onClick.AddListener(() => OnRerollSelected(2));
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

        if (firstRerollButton != null) firstRerollButton.interactable = true;
        if (secondRerollButton != null) secondRerollButton.interactable = true;
        if (thirdRerollButton != null) thirdRerollButton.interactable = true;

        // 3 kartı doldur
        RollChoiceUnique(0, false);
        RollChoiceUnique(1, false);
        RollChoiceUnique(2, false);

        RefreshTexts();
        StartCoroutine(PopScale(firstAugmentButton.transform, 1f, 1.12f, 0.12f));
        StartCoroutine(PopScale(secondAugmentButton.transform, 1f, 1.12f, 0.12f));
        StartCoroutine(PopScale(thirdAugmentButton.transform, 1f, 1.12f, 0.12f));
    }

    private Sprite GetBackgroundForRarity(Rarity r)
    {
        return r switch
        {
            Rarity.Common => commonBg,
            Rarity.Uncommon => uncommonBg,
            Rarity.Rare => rareBg,
            Rarity.Epic => epicBg,
            Rarity.Legendary => legendaryBg,
            _ => commonBg
        };
    }


    private Color GetRarityColor(Rarity r)
    {
        return r switch
        {
            Rarity.Common => commonColor,
            Rarity.Uncommon => uncommonColor,
            Rarity.Rare => rareColor,
            Rarity.Epic => epicColor,
            Rarity.Legendary => legendaryColor,
            _ => commonColor
        };
    }


    private string GetKey(UpgradeChoice c)
    {
        // ✅ rarity de dahil, yoksa aynı value/type farklı rarity clash olur
        return $"{c.rarity}_{c.type}_{c.value}";
    }

    private void RollChoiceUnique(int index, bool isReroll)
    {
        var banned = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 3; i++)
        {
            if (i == index) continue;
            if (choices[i].value > 0)
                banned.Add(GetKey(choices[i]));
        }

        if (isReroll && choices[index].value > 0)
            banned.Add(GetKey(choices[index]));

        const int maxTries = 25;
        UpgradeChoice candidate = default;

        for (int t = 0; t < maxTries; t++)
        {
            candidate = GenerateRandomChoice();
            if (!banned.Contains(GetKey(candidate)))
            {
                choices[index] = candidate;
                return;
            }
        }

        choices[index] = candidate; // nadiren buraya düşer
    }

    // -------------------- GENERATION (RARITY + LUCK) --------------------

    private UpgradeChoice GenerateRandomChoice()
    {
        UpgradeType[] possibleTypes =
        {
            UpgradeType.Strength,
            UpgradeType.MoveSpeed,
            UpgradeType.AttackSpeed,
            UpgradeType.MaxHealth,
            UpgradeType.Armor,
            UpgradeType.HpRegen,
            UpgradeType.PickupRange,
            UpgradeType.CriticalChance,
            UpgradeType.Luck,
            UpgradeType.Range
        };

        UpgradeType type = possibleTypes[Random.Range(0, possibleTypes.Length)];
        Rarity rarity = RollRarityWithLuck();

        int value = RollValueForTypeAndRarity(type, rarity);

        return new UpgradeChoice
        {
            type = type,
            rarity = rarity,
            value = value
        };
    }

    private int RollValueForTypeAndRarity(UpgradeType type, Rarity rarity)
    {
        float mult = rarity switch
        {
            Rarity.Common => 1.0f,
            Rarity.Uncommon => 1.35f,
            Rarity.Rare => 1.75f,
            Rarity.Epic => 2.35f,
            Rarity.Legendary => 3.10f,
            _ => 1.0f
        };

        float baseVal = type switch
        {
            UpgradeType.Strength => 1f,        // +1
            UpgradeType.AttackSpeed => 6f,     // +6%
            UpgradeType.MaxHealth => 5f,       // +5
            UpgradeType.MoveSpeed => 6f,       // 6 => +0.06
            UpgradeType.Armor => 1f,           // +1
            UpgradeType.HpRegen => 1f,         // 1 => +0.2/s
            UpgradeType.PickupRange => 1f,     // 1 => +1.0
            UpgradeType.CriticalChance => 6f,  // +6%
            UpgradeType.Range => 10f,          // +10%
            UpgradeType.Luck => 5f,            // +5
            _ => 1f

        };

        int v = Mathf.Max(1, Mathf.RoundToInt(baseVal * mult));

        // küçük clamp'ler (dengeyi patlatmasın)
        if (type == UpgradeType.Armor) v = Mathf.Clamp(v, 1, 5);
        if (type == UpgradeType.CriticalChance) v = Mathf.Clamp(v, 3, 30);
        if (type == UpgradeType.AttackSpeed) v = Mathf.Clamp(v, 3, 35);
        if (type == UpgradeType.Luck) v = Mathf.Clamp(v, 3, 25);
        if (type == UpgradeType.MoveSpeed) v = Mathf.Clamp(v, 3, 25); // 0.03..0.25
        if (type == UpgradeType.HpRegen) v = Mathf.Clamp(v, 1, 6);    // 0.2..1.2/s
        if (type == UpgradeType.PickupRange) v = Mathf.Clamp(v, 1, 6);// 1..6
        if (type == UpgradeType.Range) v = Mathf.Clamp(v, 5, 50);


        return v;
    }

    // -------------------- UI --------------------

    private void RefreshTexts()
    {
        SetCardUI(0, firstAugmentText, firstAugmentButton);
        SetCardUI(1, secondAugmentText, secondAugmentButton);
        SetCardUI(2, thirdAugmentText, thirdAugmentButton);

        if (augmentBg1 != null) augmentBg1.sprite = GetBackgroundForRarity(choices[0].rarity);
        if (augmentBg2 != null) augmentBg2.sprite = GetBackgroundForRarity(choices[1].rarity);
        if (augmentBg3 != null) augmentBg3.sprite = GetBackgroundForRarity(choices[2].rarity);
    }

    private void SetCardUI(int index, TMP_Text text, Button button)
    {
        if (text == null) return;

        text.text = ChoiceToString(choices[index]);
        text.color = GetRarityColor(choices[index].rarity);

        // pop anim (kart/btn üzerinde daha iyi durur)
        if (button != null)
            StartCoroutine(PopScale(button.transform, 1.0f, 1.12f, 0.12f));
    }

    private System.Collections.IEnumerator PopScale(Transform target, float baseScale, float peakScale, float duration)
    {
        if (target == null) yield break;

        // üst üste coroutine çakışmasın diye reset
        target.localScale = Vector3.one * baseScale;

        float half = duration * 0.5f;
        float t = 0f;

        // büyü
        while (t < half)
        {
            t += Time.unscaledDeltaTime; // ✅ timeScale 0 iken de çalışır
            float u = Mathf.Clamp01(t / half);
            float s = Mathf.Lerp(baseScale, peakScale, EaseOut(u));
            target.localScale = Vector3.one * s;
            yield return null;
        }

        // küçül
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime; // ✅
            float u = Mathf.Clamp01(t / half);
            float s = Mathf.Lerp(peakScale, baseScale, EaseIn(u));
            target.localScale = Vector3.one * s;
            yield return null;
        }

        target.localScale = Vector3.one * baseScale;
    }


    private float EaseOut(float x) => 1f - Mathf.Pow(1f - x, 3f);
    private float EaseIn(float x) => x * x;


    private string ChoiceToString(UpgradeChoice c)
    {
        string prefix = $"[{c.rarity}] ";

        switch (c.type)
        {
            case UpgradeType.CriticalChance:
                return $"{prefix}Critical Chance +{c.value}%";

            case UpgradeType.Luck:
                return $"{prefix}Luck +{c.value}";

            case UpgradeType.Range:
                return $"{prefix}Range +{c.value}%";

            case UpgradeType.Armor:
                return $"{prefix}Armor +{c.value}";

            case UpgradeType.HpRegen:
                return $"{prefix}HP Regen +{(0.2f * c.value):0.0}/s";

            case UpgradeType.PickupRange:
                return $"{prefix}Pickup Range +{(1.0f * c.value):0.0}";

            case UpgradeType.Strength:
                return $"{prefix}Strength +{c.value}";

            case UpgradeType.MoveSpeed:
                return $"{prefix}Move Speed +{(0.01f * c.value):0.00}";

            case UpgradeType.AttackSpeed:
                return $"{prefix}Attack Speed +{c.value}%";

            case UpgradeType.MaxHealth:
                return $"{prefix}Max Health +{c.value}";

            default:
                return $"{prefix}-";
        }
    }

    // -------------------- SELECTION --------------------

    private void OnAugmentSelected(int index)
    {
        ApplyChoiceToPlayer(choices[index]);

        gameObject.SetActive(false);
        if (gameManager != null)
            gameManager.OnUpgradeChosen();
    }

    private void ApplyChoiceToPlayer(UpgradeChoice c)
    {
        if (player == null) return;

        switch (c.type)
        {
            case UpgradeType.Range:
                player.ChangeRange(c.value / 100f); // %10 => 0.10
                break;

            case UpgradeType.CriticalChance:
                player.ChangeCritChance(c.value);
                break;

            case UpgradeType.Luck:
                player.ChangeLuck(c.value);
                break;

            case UpgradeType.Armor:
                player.ChangeArmor(c.value);
                break;

            case UpgradeType.HpRegen:
                player.ChangeHpRegen(0.2f * c.value);
                break;

            case UpgradeType.PickupRange:
                player.ChangePickupRange(1.0f * c.value);
                break;

            case UpgradeType.Strength:
                player.ChangeStrength(c.value);
                break;

            case UpgradeType.MoveSpeed:
                player.ChangeMoveSpeed(0.01f * c.value); // ✅ FIX
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

        PlayRerollJuice(index);

        RollChoiceUnique(index, true);
        rerollUsed[index] = true;

        if (index == 0 && firstRerollButton != null) firstRerollButton.interactable = false;
        if (index == 1 && secondRerollButton != null) secondRerollButton.interactable = false;
        if (index == 2 && thirdRerollButton != null) thirdRerollButton.interactable = false;

        RefreshTexts();
        if (index == 0) StartCoroutine(PopScale(firstAugmentButton.transform, 1f, 1.12f, 0.12f));
        if (index == 1) StartCoroutine(PopScale(secondAugmentButton.transform, 1f, 1.12f, 0.12f));
        if (index == 2) StartCoroutine(PopScale(thirdAugmentButton.transform, 1f, 1.12f, 0.12f));
    }

    private void PlayRerollJuice(int index)
    {
        Transform card = null;
        Image flash = null;

        if (index == 0) { card = firstAugmentButton.transform; flash = flash1; }
        if (index == 1) { card = secondAugmentButton.transform; flash = flash2; }
        if (index == 2) { card = thirdAugmentButton.transform; flash = flash3; }

        if (card != null) StartCoroutine(CardRerollAnim(card, flash));
    }

    private System.Collections.IEnumerator CardRerollAnim(Transform card, Image flash)
    {
        float dur = 0.12f / uiAnimSpeed;

        // press: 1.00 -> 0.92 -> 1.03 -> 1.00
        yield return StartCoroutine(ScaleTo(card, 1.00f, 0.92f, dur * 0.35f));
        yield return StartCoroutine(ScaleTo(card, 0.92f, 1.03f, dur * 0.35f));
        yield return StartCoroutine(ScaleTo(card, 1.03f, 1.00f, dur * 0.30f));

        // shake + flash paralel değil, ardışık (istersen paralel de yaparız)
        yield return StartCoroutine(Shake(card, 7f, 0.08f / uiAnimSpeed));
        if (flash != null) yield return StartCoroutine(Flash(flash, 0.10f / uiAnimSpeed));
    }


    private System.Collections.IEnumerator ScaleTo(Transform target, float from, float to, float duration)
    {
        if (target == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            float s = Mathf.Lerp(from, to, EaseOut(u));
            target.localScale = Vector3.one * s;
            yield return null;
        }
        target.localScale = Vector3.one * to;
    }


    private System.Collections.IEnumerator Shake(Transform t, float strength, float duration)
    {
        if (t == null) yield break;

        RectTransform rt = t as RectTransform;
        if (rt == null)
        {
            // fallback (3D objeyse)
            Vector3 start = t.localPosition;
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float x = Random.Range(-1f, 1f) * strength;
                float y = Random.Range(-1f, 1f) * strength;
                t.localPosition = start + new Vector3(x, y, 0f);
                yield return null;
            }
            t.localPosition = start;
            yield break;
        }

        Vector2 startPos = rt.anchoredPosition;
        float tt = 0f;

        while (tt < duration)
        {
            tt += Time.unscaledDeltaTime;
            float x = Random.Range(-1f, 1f) * strength;
            float y = Random.Range(-1f, 1f) * strength;
            rt.anchoredPosition = startPos + new Vector2(x, y);
            yield return null;
        }

        rt.anchoredPosition = startPos;
    }


    private System.Collections.IEnumerator Flash(Image img, float duration)
    {
        if (img == null) yield break;

        // alpha 0 -> 0.35 -> 0
        Color c = img.color;
        c.a = 0f;
        img.color = c;

        float half = duration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0f, 0.35f, t / half);
            img.color = c;
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0.35f, 0f, t / half);
            img.color = c;
            yield return null;
        }

        c.a = 0f;
        img.color = c;
    }


    // -------------------- RARITY ROLL (LUCK) --------------------

    private Rarity RollRarityWithLuck()
    {
        float L = (player != null) ? player.luck : 0f;
        float t = L / 100f; // 0..1

        float common = 100f;
        float uncom = 45f;
        float rare = 18f;
        float epic = 5f;
        float leg = 1f;

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
}
