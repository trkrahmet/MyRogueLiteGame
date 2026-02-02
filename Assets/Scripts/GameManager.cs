using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private enum WaveState
    {
        Combat,
        Elite,
        Upgrade,
        Shop
        // Boss state’i Adım 3’te buraya ekleyeceğiz
    }

    [Header("Wave Settings")]
    public float waveDuration = 60f;
    public int currentWaveLevel = 1;

    [Header("Gold Settings")]
    [SerializeField] private float goldGrowthPerWave = 0.12f; // her wave %12 artış


    [Header("Kill-Based Wave")]
    public int baseKillCount = 8;          // Wave 1
    public int killIncreasePerWave = 4;    // Her wave artışı

    private int currentKills = 0;
    private int targetKills = 0;

    [Header("Elite Phase")]
    public float eliteTimeLimit = 20f;
    private float eliteTimer = 0f;
    private bool eliteAlive = false;

    public System.Action OnEliteStarted;
    public System.Action OnEliteEnded; // success/fail (ikisini de kapatır)

    private Enemy currentEliteEnemy;


    private float waveTimer;

    [Header("References")]
    public EnemySpawner spawner;
    public Player player;
    [SerializeField] private ShopPanel shopPanel;
    [SerializeField] private UpgradePanel upgradePanel;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private HealthUI healthUI;

    [Header("UI (TMP)")]
    public TMP_Text waveText;
    public TMP_Text timerText;
    public TMP_Text levelText;

    private WaveState state = WaveState.Combat;

    // Wave sonunda kaç upgrade hakkı kaldı?
    private int remainingUpgradePicks = 0;

    private void Start()
    {
        // Refs
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (player == null) player = FindFirstObjectByType<Player>();
        if (shopPanel == null) shopPanel = FindFirstObjectByType<ShopPanel>();
        if (upgradePanel == null) upgradePanel = FindFirstObjectByType<UpgradePanel>();
        if (healthUI == null) healthUI = FindFirstObjectByType<HealthUI>();

        if (spawner != null)
            spawner.OnFinalEnemySpawned += HandleFinalEnemySpawned;

        // Panel başlangıç
        shopPanel?.CloseShop();
        if (upgradePanel != null) upgradePanel.gameObject.SetActive(false);

        StartWave();
        RefreshUI();
    }

    private void Update()
    {
        switch (state)
        {
            case WaveState.Combat:
                TickCombat();
                break;

            case WaveState.Elite:
                TickElite();
                break;

            case WaveState.Upgrade:
            case WaveState.Shop:
                break;
        }

        RefreshUI();
    }

    public void OnEnemyKilled(Enemy e)
    {
        // 1) GOLD ver
        GiveGoldForEnemy(e);

        // 2) Kill sayacı (Combat ise)
        if (state == WaveState.Combat)
        {
            currentKills++;
            RefreshUI();
            return;
        }

        // 3) Elite fazındaysa final enemy öldü say
        if (state == WaveState.Elite)
        {
            eliteAlive = false;
            RefreshUI();
            return;
        }
    }

    private void GiveGoldForEnemy(Enemy e)
    {
        if (player == null) return;

        int baseGold = (e != null) ? Mathf.Max(0, e.baseGold) : 1;
        float mult = 1f + (currentWaveLevel - 1) * goldGrowthPerWave;

        int gained = Mathf.Max(0, Mathf.RoundToInt(baseGold * mult));
        player.AddGold(gained);
    }


    private void HandleFinalEnemySpawned(Enemy e)
    {
        currentEliteEnemy = e;
        // Boss UI burada tetiklenecek (aşağıda)
    }


    // -------------------- STATE: COMBAT --------------------

    private void StartWave()
    {
        SetState(WaveState.Combat);
        currentKills = 0;
        targetKills = baseKillCount + (currentWaveLevel - 1) * killIncreasePerWave;


        waveTimer = waveDuration;

        if (spawner != null)
        {
            spawner.SetSpawnLimit(targetKills);
            spawner.ApplyWaveSettings(currentWaveLevel);
            spawner.spawningEnabled = true;
        }

        // Wave başında canı full'lemek istiyorsan:
        if (player != null) player.HealToFull();

        healthUI?.SetVisible(true);
        Time.timeScale = 1f;
    }

    public int GetTargetKills()
    {
        return targetKills;
    }


    private void TickCombat()
    {
        // waveTimer -= Time.deltaTime;

        // if (waveTimer <= 0f)
        // {
        //     EndWave_ToIntermission();
        // }

        if (currentKills >= targetKills && spawner.AreAllEnemiesSpawned())
        {
            StartElitePhase();
        }
    }

    private void StartElitePhase()
    {
        SetState(WaveState.Elite);

        // Normal spawn kapat
        if (spawner != null) spawner.spawningEnabled = false;

        // Sahayı temizle (senin tasarımın: wave temizlenince arena boss’a dönüyor)
        spawner?.ClearAllEnemies();
        ClearAllXpOrbs();
        ClearAllWarnings();

        // Elite timer
        eliteTimer = eliteTimeLimit;
        eliteAlive = true;

        // Elite spawn (Spawner'a ekleyeceğiz)
        if (spawner != null)
            spawner.SpawnFinalEnemyForWave(currentWaveLevel);


        // savaş devam etsin
        Time.timeScale = 1f;
        healthUI?.SetVisible(true);
        OnEliteStarted?.Invoke();
    }

    private void TickElite()
    {
        if (!eliteAlive)
        {
            EndElite_Success();
            return;
        }

        eliteTimer -= Time.deltaTime;

        if (eliteTimer <= 0f)
        {
            EndElite_Fail();
        }
    }

    // public void RegisterEliteKilled()
    // {
    //     if (state != WaveState.Elite) return;
    //     eliteAlive = false;
    // }

    private void GoToIntermission()
    {
        // Oyun durdur
        Time.timeScale = 0f;
        healthUI?.SetVisible(false);

        remainingUpgradePicks = (player != null) ? player.pendingUpgradePoints : 0;

        if (remainingUpgradePicks > 0)
            OpenUpgrade();
        else
            OpenShop();
    }

    private void EndElite_Success()
    {
        OnEliteEnded?.Invoke();     // ✅ overlay + zoom geri dönsün
        currentEliteEnemy = null;

        GoToIntermission();         // sonra pause
    }

    private void EndElite_Fail()
    {
        OnEliteEnded?.Invoke();     // ✅ fail'de de kapanmalı
        currentEliteEnemy = null;

        Debug.Log("Elite failed (time out). Run over for now (retry later).");
        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }



    public bool IsKillTargetReached()
    {
        return currentKills >= targetKills;
    }


    private void EndWave_ToIntermission()
    {
        // Combat bitiş
        if (spawner != null) spawner.spawningEnabled = false;

        // Saha temizliği (şimdilik eski akışın aynısı)
        spawner?.ClearAllEnemies();
        ClearAllXpOrbs();
        ClearAllWarnings();

        // Oyun durdur (UI için istersen sonra unscaled’a geçeriz)
        Time.timeScale = 0f;
        healthUI?.SetVisible(false);

        // Kaç upgrade hakkı var?
        remainingUpgradePicks = (player != null) ? player.pendingUpgradePoints : 0;

        if (remainingUpgradePicks > 0)
            OpenUpgrade();
        else
            OpenShop();
    }

    // -------------------- STATE: UPGRADE --------------------

    private void OpenUpgrade()
    {
        SetState(WaveState.Upgrade);

        healthUI?.SetVisible(false);
        if (upgradePanel != null) upgradePanel.Open();
    }

    public void OnUpgradeChosen()
    {
        // Burayı UpgradePanel çağırıyor (senin mevcut akışın)
        remainingUpgradePicks = Mathf.Max(0, remainingUpgradePicks - 1);
        if (player != null) player.pendingUpgradePoints = Mathf.Max(0, player.pendingUpgradePoints - 1);

        if (remainingUpgradePicks > 0)
        {
            OpenUpgrade(); // tekrar 3 kart roll
        }
        else
        {
            OpenShop();
        }
    }

    // -------------------- STATE: SHOP --------------------

    private void OpenShop()
    {
        SetState(WaveState.Shop);

        healthUI?.SetVisible(false);
        shopPanel?.OpenShop();
    }

    // ShopPanel Continue butonundan çağrılacak
    public void ContinueForNextLevel()
    {
        // shop’tan çıkarken
        shopPanel?.CloseShop();
        if (upgradePanel != null) upgradePanel.gameObject.SetActive(false);

        // bir sonraki wave hazırlığı
        ClearAllXpOrbs();
        ClearAllWarnings();
        ResetPlayerToCenter();

        currentWaveLevel++;

        // tekrar combat
        StartWave();
    }

    // -------------------- HELPERS --------------------

    private void SetState(WaveState newState)
    {
        state = newState;
    }

    private void RefreshUI()
    {
        if (waveText != null) waveText.text = $"Wave: {currentWaveLevel}";
        if (levelText != null && player != null) levelText.text = $"Level: {player.playerLevel}";

        if (timerText == null) return;

        if (state == WaveState.Combat)
            timerText.text = $"Kills: {currentKills} / {targetKills}";

        else if (state == WaveState.Upgrade)
            timerText.text = "Upgrade";

        else if (state == WaveState.Elite)
            timerText.text = $"ELITE: {Mathf.CeilToInt(eliteTimer)}s";
        else
            timerText.text = "Shop";
    }

    private void ClearAllXpOrbs()
    {
        var xps = GameObject.FindGameObjectsWithTag("XP");
        for (int i = 0; i < xps.Length; i++)
            Destroy(xps[i]);
    }

    private void ClearAllWarnings()
    {
        var xps = GameObject.FindGameObjectsWithTag("Warning");
        for (int i = 0; i < xps.Length; i++)
            Destroy(xps[i]);
    }

    private void ResetPlayerToCenter()
    {
        if (player == null) return;

        if (playerSpawnPoint != null)
            player.transform.position = playerSpawnPoint.position;
        else
            player.transform.position = Vector3.zero;
    }

    // public void RegisterEnemyKill()
    // {
    //     if (state != WaveState.Combat) return;
    //     currentKills++;
    //     RefreshUI();
    // }
}
