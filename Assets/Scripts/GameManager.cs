using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveDuration = 60f;
    public int currentWaveLevel = 1;
    public float waveTimer;
    bool inCombat = true;

    [Header("References")]
    public EnemySpawner spawner;
    public Player player;
    [SerializeField] ShopPanel shopPanel;
    [SerializeField] UpgradePanel upgradePanel;

    [Header("UI (TMP)")]
    public TMP_Text waveText;
    public TMP_Text timerText;
    public TMP_Text levelText;

    public int remainingUpgradePicks = 0;

    private void Start()
    {
        if (shopPanel == null) shopPanel = FindFirstObjectByType<ShopPanel>();
        if (shopPanel != null) shopPanel.CloseShop();

        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (player == null) player = FindFirstObjectByType<Player>();

        waveTimer = waveDuration;

        StartCombat();
        UpdateUI();
    }

    private void Update()
    {
        if (!inCombat)
        {
            return;
        }

        waveTimer -= Time.deltaTime;
        if (waveTimer <= 0f)
        {
            EndCombat();
            spawner.ClearAllEnemies();
            return;
        }
        UpdateUI();
    }

    private void StartCombat()
    {
        inCombat = true;
        waveTimer = waveDuration;

        if (spawner != null) { spawner.spawningEnabled = true; }

        Time.timeScale = 1f;
    }

    private void EndCombat()
    {
        inCombat = false;

        if (spawner != null) { spawner.spawningEnabled = false; }

        Time.timeScale = 0f;

        UpdateUI();

        remainingUpgradePicks = player != null ? player.pendingUpgradePoints : 0;

        if (remainingUpgradePicks > 0)
        {
            ShowNextUpgradePick();
        }
        else
        {
            shopPanel.OpenShop();
        }

        Debug.Log($"Wave {currentWaveLevel} ended. Prepare for upgrades!");
    }

    private void ShowNextUpgradePick()
    {
        upgradePanel.Open();
    }

    public void OnUpgradeChosen()
    {
        remainingUpgradePicks--;
        player.pendingUpgradePoints--;

        if (remainingUpgradePicks > 0)
        {
            ShowNextUpgradePick();
        }
        else
        {
            shopPanel.OpenShop();
        }
    }

    private void UpdateUI()
    {
        if (waveText != null) { waveText.text = $"Wave: {currentWaveLevel}"; }
        if (timerText != null) timerText.text = $"Time: {Mathf.CeilToInt(waveTimer)}";
        if (levelText != null && spawner != null) { levelText.text = $"Level: {player.playerLevel}"; }
    }

    public void ContinueForNextLevel()
    {
        currentWaveLevel++;
        spawner.ApplyWaveSettings(currentWaveLevel);
        StartCombat();
    }
}
