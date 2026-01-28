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
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private HealthUI healthUI;


    [Header("UI (TMP)")]
    public TMP_Text waveText;
    public TMP_Text timerText;
    public TMP_Text levelText;

    public int remainingUpgradePicks = 0;

    private void Start()
    {
        if (shopPanel == null) shopPanel = FindFirstObjectByType<ShopPanel>();
        if (shopPanel != null) shopPanel.CloseShop();
        if (healthUI == null) healthUI = FindFirstObjectByType<HealthUI>();
        if (upgradePanel == null) upgradePanel = FindFirstObjectByType<UpgradePanel>();
        if (upgradePanel != null) upgradePanel.gameObject.SetActive(false);


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
            ClearAllXpOrbs();

            return;
        }
        UpdateUI();
    }

    private void StartCombat()
    {
        inCombat = true;

        if (player != null) { player.HealToFull(); }


        waveTimer = waveDuration;

        if (spawner != null) { spawner.spawningEnabled = true; }

        Time.timeScale = 1f;
        healthUI?.SetVisible(true);
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
            healthUI?.SetVisible(false);
        }

        healthUI?.SetVisible(false);
        Debug.Log($"Wave {currentWaveLevel} ended. Prepare for upgrades!");
    }

    private void ShowNextUpgradePick()
    {
        upgradePanel.Open();
        healthUI?.SetVisible(false);
    }

    public void OnUpgradeChosen()
    {
        remainingUpgradePicks = Mathf.Max(0, remainingUpgradePicks - 1);

        if (player != null)
            player.pendingUpgradePoints = Mathf.Max(0, player.pendingUpgradePoints - 1);

        if (remainingUpgradePicks > 0)
        {
            ShowNextUpgradePick();
        }
        else
        {
            shopPanel?.OpenShop();
            healthUI?.SetVisible(false);
        }
    }


    private void UpdateUI()
    {
        if (waveText != null) { waveText.text = $"Wave: {currentWaveLevel}"; }
        if (timerText != null) timerText.text = $"Time: {Mathf.CeilToInt(waveTimer)}";
        if (levelText != null && player != null) { levelText.text = $"Level: {player.playerLevel}"; }
    }

    public void ContinueForNextLevel()
    {
        // ✅ HER ŞEYDEN ÖNCE: oyunu çöz (timeScale 0 kalmasın)
        Time.timeScale = 1f;
        inCombat = true;

        // ✅ Ref’leri garantiye al (sahnede değişmiş olabilir)
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (player == null) player = FindFirstObjectByType<Player>();
        if (shopPanel == null) shopPanel = FindFirstObjectByType<ShopPanel>();
        if (upgradePanel == null) upgradePanel = FindFirstObjectByType<UpgradePanel>();

        // ✅ Panelleri kapat (bazı durumlarda açık kalabiliyor)
        shopPanel?.CloseShop();
        if (upgradePanel != null) upgradePanel.gameObject.SetActive(false);

        ClearAllXpOrbs();
        ResetPlayerToCenter();

        currentWaveLevel++;

        // ✅ spawner ayarları hata verirse bile combat başlasın
        if (spawner != null)
        {
            try
            {
                spawner.ApplyWaveSettings(currentWaveLevel);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ApplyWaveSettings error: {e}");
            }
        }
        else
        {
            Debug.LogError("Spawner is NULL in ContinueForNextLevel!");
        }

        StartCombat();
        UpdateUI();
    }



    private void ClearAllXpOrbs()
    {
        // XP orb’larının tag’i "XP" ise:
        var xps = GameObject.FindGameObjectsWithTag("XP");
        for (int i = 0; i < xps.Length; i++)
            Destroy(xps[i]);
    }

    private void ResetPlayerToCenter()
    {
        if (player == null) return;

        if (playerSpawnPoint != null)
            player.transform.position = playerSpawnPoint.position;
        else
            player.transform.position = Vector3.zero; // spawn point bağlamadıysan 0,0
    }
}
