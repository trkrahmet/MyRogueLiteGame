using System.Collections;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private enum WaveState
    {
        Combat,
        Elite,
        ChestReward,
        Upgrade,
        Shop
        // Boss state’i Adım 3’te buraya ekleyeceğiz
    }

    [Header("Wave Settings")]
    public float waveDuration = 60f;
    public int currentWaveLevel = 1;

    [Header("UI - Animated Toast")]
    [SerializeField] private RectTransform toastRoot;
    [SerializeField] private CanvasGroup toastGroup;
    [SerializeField] private TMP_Text toastText;

    [SerializeField] private float toastInTime = 0.18f;
    [SerializeField] private float toastHoldTime = 1.4f;
    [SerializeField] private float toastOutTime = 0.22f;

    [SerializeField] private float toastStartScale = 0.85f;
    [SerializeField] private float toastOvershootScale = 1.05f;

    private Coroutine toastRoutine;

    [Header("Gold Settings")]
    [SerializeField] private float goldGrowthPerWave = 0.12f; // her wave %12 artış

    [Header("Pending Rewards")]
    [SerializeField] private bool usePendingRewards = true;
    [SerializeField] private float failCommitRatio = 0.5f;

    private int pendingGold = 0;
    private int pendingXp = 0;

    [Header("Elite Chest Reward")]
    [SerializeField] private GameObject eliteChestPrefab;
    [SerializeField] private float chestSpawnDelay = 0.35f;
    [SerializeField] private ChestRewardPanel chestRewardPanel;
    private RewardChest activeChest;
    private ShopPanel.ChestOfferData chestOffer;
    private Vector3 lastEliteDeathPos;

    private Coroutine chestSpawnRoutine;
    private bool chestSpawnScheduled;

    private bool transitionLocked = false;
    private Coroutine transitionRoutine;

    [SerializeField] private Transform eliteCenterPoint; // inspector’dan arena merkezini ver
    private Transform playerTransform;

    [Header("Elite Arena Spawn")]
    [SerializeField] private Vector2 elitePlayerCenter = Vector2.zero;  // (0,0)
    [SerializeField] private Vector2 bossSpawnDirection = Vector2.up; // örn: yukarı
    [SerializeField] private float bossSpawnDistance = 6f;            // mesafe
    [SerializeField] private float bossSpawnSideJitter = 1.2f;        // sağ-sol küçük sapma

    [Header("Transition Delays")]
    [SerializeField] private float preEliteDelay = 0.8f;      // Combat bitince Elite'a geçmeden önce bekleme
    [SerializeField] private float preUpgradeDelay = 1.0f;    // Boss ölünce Upgrade/Shop'a geçmeden önce bekleme

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

            case WaveState.ChestReward:
                // burada hiçbir şey yapmana gerek yok (input panelde)
                break;


            case WaveState.Upgrade:
            case WaveState.Shop:
                break;
        }
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
            if (e != null) lastEliteDeathPos = e.transform.position;
            eliteAlive = false;
            RefreshUI();
            return;
        }
    }

    private void GiveGoldForEnemy(Enemy e)
    {
        int baseGold = (e != null) ? Mathf.Max(0, e.baseGold) : 1;
        float mult = 1f + (currentWaveLevel - 1) * goldGrowthPerWave;

        int gained = Mathf.Max(0, Mathf.RoundToInt(baseGold * mult));
        AddPendingGold(gained);
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

        if (player != null)
        {
            player.transform.position = Vector3.zero;
            player.HealToFull();
            player.RespawnVisual(playerSpawnPoint.position);
        }

        pendingGold = 0;
        pendingXp = 0;
        currentKills = 0;
        targetKills = baseKillCount + (currentWaveLevel - 1) * killIncreasePerWave;


        waveTimer = waveDuration;

        if (spawner != null)
        {
            spawner.SetSpawnLimit(targetKills);
            spawner.ApplyWaveSettings(currentWaveLevel);
            spawner.spawningEnabled = true;
        }



        healthUI?.SetVisible(true);
        Time.timeScale = 1f;
    }

    // public int GetTargetKills()
    // {
    //     return targetKills;
    // }


    private void TickCombat()
    {
        // waveTimer -= Time.deltaTime;

        // if (waveTimer <= 0f)
        // {
        //     EndWave_ToIntermission();
        // }

        if (!transitionLocked && currentKills >= targetKills)
        {
            transitionLocked = true;

            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(Co_StartEliteAfterDelay());
        }

    }

    private void CachePlayer()
    {
        if (playerTransform != null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) playerTransform = go.transform;
    }

    private void StartElitePhase()
    {
        SetState(WaveState.Elite);

        // Normal spawn kapat
        if (spawner != null) spawner.spawningEnabled = false;

        // Sahayı temizle (senin tasarımın: wave temizlenince arena boss’a dönüyor)
        spawner?.ClearAllEnemies();
        // ClearAllXpOrbs();
        ClearAllWarnings();

        // Elite timer
        eliteTimer = eliteTimeLimit;
        eliteAlive = true;

        // Elite spawn (Spawner'a ekleyeceğiz)
        if (spawner != null)
        {
            TeleportPlayerToEliteCenter();
            Vector3 bossPos = GetBossSpawnPosition();
            spawner.SpawnFinalEnemyForWaveAtPosition(currentWaveLevel, bossPos);
        }

        // savaş devam etsin
        Time.timeScale = 1f;
        healthUI?.SetVisible(true);
        OnEliteStarted?.Invoke();
    }

    private Vector3 GetBossSpawnPosition()
    {
        CachePlayer();
        if (playerTransform == null) return Vector3.zero;

        Vector2 dir = bossSpawnDirection.sqrMagnitude < 0.001f ? Vector2.up : bossSpawnDirection.normalized;

        // player'ın önüne + hafif sağ-sol random
        Vector2 side = new Vector2(-dir.y, dir.x);
        float jitter = Random.Range(-bossSpawnSideJitter, bossSpawnSideJitter);

        Vector2 pos2D = (Vector2)playerTransform.position + dir * bossSpawnDistance + side * jitter;
        return new Vector3(pos2D.x, pos2D.y, 0f);
    }


    private void TeleportPlayerToEliteCenter()
    {
        CachePlayer();
        if (playerTransform == null) return;

        playerTransform.position = new Vector3(elitePlayerCenter.x, elitePlayerCenter.y, playerTransform.position.z);
    }

    private IEnumerator Co_StartEliteAfterDelay()
    {
        // Loot/xp toplamak için pencere
        yield return new WaitForSeconds(preEliteDelay);

        // Bu süre içinde state değiştiyse iptal
        if (state != WaveState.Combat)
        {
            transitionLocked = false;
            yield break;
        }

        // Elite'a geçerken artık sahayı temizleyebiliriz
        ClearAllXpOrbs();
        ClearAllWarnings();

        StartElitePhase();

        transitionLocked = false;
    }


    private void TickElite()
    {
        if (!eliteAlive)
        {
            // ✅ Elite bitti ama state hâlâ Elite ise 1 kez transition başlat
            if (!transitionLocked)
            {
                transitionLocked = true;

                if (transitionRoutine != null) StopCoroutine(transitionRoutine);
                transitionRoutine = StartCoroutine(Co_EndEliteSuccessAfterDelay());
            }
            return;
        }

        eliteTimer -= Time.deltaTime;
        RefreshUI();
        if (eliteTimer <= 0f)
            EndElite_Fail();
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

    private IEnumerator Co_EndEliteSuccessAfterDelay()
    {
        yield return new WaitForSeconds(preUpgradeDelay);

        if (state != WaveState.Elite)
        {
            transitionLocked = false;
            yield break;
        }

        // ✅ Elite success -> ChestReward
        EnterChestReward();

        // ⚠️ transitionLocked burada false olmasın; chest bitince açacağız
    }

    private void EnterChestReward()
    {
        CommitPending(1f); // ✅ Elite başarı: tüm pending %100 aktar

        SetState(WaveState.ChestReward);
        Time.timeScale = 1f;
        healthUI?.SetVisible(true);

        OnEliteEnded?.Invoke();
        currentEliteEnemy = null;
        player.ResetVisualState();

        // ✅ Zaten sahnede chest varsa tekrar spawnlama
        if (activeChest != null) return;

        // ✅ Daha önce spawn planlandıysa tekrar planlama
        if (chestSpawnScheduled) return;
        chestSpawnScheduled = true;

        // ✅ Eski coroutine varsa iptal et (güvence)
        if (chestSpawnRoutine != null) StopCoroutine(chestSpawnRoutine);
        chestSpawnRoutine = StartCoroutine(Co_SpawnChestOnce());
    }
    // private void EnterChestReward()
    // {
    //     // Elite sunumu bitsin (overlay/arena/boss ui kapansın)
    //     OnEliteEnded?.Invoke();

    //     currentEliteEnemy = null;
    //     player.ResetVisualState();

    //     SetState(WaveState.ChestReward);

    //     // oyun devam etsin, chest animasyonu gözüksün
    //     Time.timeScale = 1f;
    //     healthUI?.SetVisible(true);

    //     if (transitionRoutine != null) StopCoroutine(transitionRoutine);
    //     transitionRoutine = StartCoroutine(Co_SpawnChest());
    // }

    private IEnumerator Co_SpawnChestOnce()
    {
        yield return new WaitForSeconds(chestSpawnDelay);

        // ChestReward state’inde değilsek iptal
        if (state != WaveState.ChestReward)
        {
            chestSpawnScheduled = false;
            yield break;
        }

        // ✅ Eğer bu arada chest oluştuysa tekrar basma
        if (activeChest != null)
        {
            chestSpawnScheduled = false;
            yield break;
        }

        if (eliteChestPrefab == null)
        {
            chestSpawnScheduled = false;
            transitionLocked = false;
            GoToIntermission();
            yield break;
        }

        var go = Instantiate(eliteChestPrefab, lastEliteDeathPos, Quaternion.identity);
        activeChest = go.GetComponent<RewardChest>();
        if (activeChest != null) activeChest.Init(this);

        // ✅ OFFER + RARITY AYARINI BURAYA TAŞI (comment’ten çıkar)
        chestOffer = shopPanel.RollChestOffer(allowWeapons: false);
        if (activeChest != null)
            activeChest.SetRarityIndex(chestOffer.rarityIndex);

        chestSpawnScheduled = false;
    }


    // private void SpawnChestReward()
    // {
    //     OnEliteEnded?.Invoke();
    //     currentEliteEnemy = null;
    //     player.ResetVisualState();

    //     if (eliteChestPrefab == null)
    //     {
    //         GoToIntermission();
    //         return;
    //     }

    //     var go = Instantiate(eliteChestPrefab, lastEliteDeathPos, Quaternion.identity);
    //     activeChest = go.GetComponent<RewardChest>();
    //     if (activeChest != null) activeChest.Init(this);

    //     // ✅ ödülü shop havuzundan çek
    //     chestOffer = shopPanel.RollChestOffer(allowWeapons: false);

    //     // ✅ FX için rarity index’i chest’e ver
    //     if (activeChest != null)
    //         activeChest.SetRarityIndex(chestOffer.rarityIndex);

    //     // (Opsiyonel) UI panel arkaplan rengi için saklamak istersen:
    //     // cachedChestRarityColor = shopPanel.GetRarityColorByIndex(chestOffer.rarityIndex);
    // }

    // private void EndElite_Success()
    // {
    //     OnEliteEnded?.Invoke();     // ✅ overlay + zoom geri dönsün
    //     currentEliteEnemy = null;
    //     player.ResetVisualState();

    //     GoToIntermission();         // sonra pause
    // }

    private void EndElite_Fail()
    {
        if (transitionLocked) return;
        transitionLocked = true;

        CommitPending(failCommitRatio);

        CleanupAfterEliteFailOrDeath();  // ✅ EKLE

        OnEliteEnded?.Invoke();

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(Co_FailFlow());
    }

    public bool IsKillTargetReached()
    {
        return currentKills >= targetKills;
    }

    // private void EndWave_ToIntermission()
    // {
    //     // Combat bitiş
    //     if (spawner != null) spawner.spawningEnabled = false;

    //     // Saha temizliği (şimdilik eski akışın aynısı)
    //     spawner?.ClearAllEnemies();
    //     ClearAllXpOrbs();
    //     ClearAllWarnings();

    //     // Oyun durdur (UI için istersen sonra unscaled’a geçeriz)
    //     Time.timeScale = 0f;
    //     healthUI?.SetVisible(false);

    //     // Kaç upgrade hakkı var?
    //     remainingUpgradePicks = (player != null) ? player.pendingUpgradePoints : 0;

    //     if (remainingUpgradePicks > 0)
    //         OpenUpgrade();
    //     else
    //         OpenShop();
    // }

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

        else if (state == WaveState.ChestReward)
            timerText.text = "Open the chest!";

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

    // private IEnumerator Co_SpawnChest()
    // {
    //     yield return new WaitForSeconds(chestSpawnDelay);

    //     if (eliteChestPrefab == null)
    //     {
    //         // chest yoksa direkt intermission'a geç
    //         GoToIntermission();
    //         yield break;
    //     }

    //     var go = Instantiate(eliteChestPrefab, lastEliteDeathPos, Quaternion.identity);
    //     activeChest = go.GetComponent<RewardChest>();

    //     if (activeChest != null)
    //         activeChest.Init(this);
    //     else
    //         Debug.LogWarning("Elite chest prefab has no RewardChest component!");
    // }

    public void OnChestOpened()
    {
        if (chestRewardPanel == null)
        {
            GoToIntermission();
            return;
        }

        // Panel açılınca oyunu durduralım (UI rahat)
        Time.timeScale = 0f;

        int sellValue = shopPanel.GetSellValue(chestOffer);
        Color rarityColor = shopPanel.GetRarityColorByIndex(chestOffer.rarityIndex);

        bool canTake = true;
        if (chestOffer.isWeapon && player != null)
            canTake = player.HasFreeWeaponSlot();

        chestRewardPanel.Show(
            chestOffer,
            rarityColor,
            sellValue,
            canTake,
            onTake: () =>
            {
                bool ok = shopPanel.ApplyChestOffer(chestOffer);
                if (!ok)
                {
                    // silah slotu yoksa vs: direkt sat
                    player.AddGold(sellValue);
                }
                CloseChestAndContinue();
            },
            onSell: () =>
            {
                player.AddGold(sellValue);
                CloseChestAndContinue();
            }
        );
    }

    private void CloseChestAndContinue()
    {
        chestRewardPanel.Hide();

        if (activeChest != null)
            Destroy(activeChest.gameObject);

        activeChest = null;

        transitionLocked = false;
        GoToIntermission();
    }

    public void AddPendingGold(int amount)
    {
        if (!usePendingRewards)
        {
            player?.AddGold(amount);
            return;
        }

        if (amount <= 0) return;
        pendingGold += amount;
    }

    public void AddPendingXp(int amount)
    {
        if (!usePendingRewards)
        {
            player?.AddXp(amount);
            return;
        }

        if (amount <= 0) return;
        pendingXp += amount;
    }

    private void CommitPending(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        int goldToGive = Mathf.RoundToInt(pendingGold * ratio);
        int xpToGive = Mathf.RoundToInt(pendingXp * ratio);

        if (player != null)
        {
            if (goldToGive > 0) player.AddGold(goldToGive);
            if (xpToGive > 0) player.AddXp(xpToGive);
        }

        pendingGold = 0;
        pendingXp = 0;
    }

    public void OnPlayerDied()
    {
        if (transitionLocked) return;
        transitionLocked = true;
        player.OnDeathVisual();

        CommitPending(failCommitRatio);

        CleanupAfterEliteFailOrDeath();  // ✅ EKLE

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(Co_FailFlow());
    }

    private void ShowAnimatedToast(string msg)
    {
        if (toastRoot == null || toastGroup == null || toastText == null) return;

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(Co_Toast(msg));
    }

    private IEnumerator Co_Toast(string msg)
    {
        toastRoot.gameObject.SetActive(true);
        toastText.text = msg;

        // başlangıç state
        toastGroup.alpha = 0f;
        toastRoot.localScale = Vector3.one * toastStartScale;

        // --- IN (pop + fade) ---
        float t = 0f;
        while (t < toastInTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / toastInTime);

            // alpha
            toastGroup.alpha = a;

            // pop scale (2 aşamalı: start->overshoot->1)
            float s;
            if (a < 0.7f)
            {
                float p = a / 0.7f;
                s = Mathf.Lerp(toastStartScale, toastOvershootScale, p);
            }
            else
            {
                float p = (a - 0.7f) / 0.3f;
                s = Mathf.Lerp(toastOvershootScale, 1f, p);
            }

            toastRoot.localScale = Vector3.one * s;
            yield return null;
        }

        toastGroup.alpha = 1f;
        toastRoot.localScale = Vector3.one;

        // --- HOLD ---
        yield return new WaitForSecondsRealtime(toastHoldTime);

        // --- OUT (fade + hafif küçülme) ---
        t = 0f;
        while (t < toastOutTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / toastOutTime);

            toastGroup.alpha = 1f - a;
            float s = Mathf.Lerp(1f, 0.95f, a);
            toastRoot.localScale = Vector3.one * s;

            yield return null;
        }

        toastGroup.alpha = 0f;
        toastRoot.localScale = Vector3.one;

        toastRoot.gameObject.SetActive(false);
        toastRoutine = null;
    }

    private IEnumerator Co_FailFlow()
    {
        ShowAnimatedToast("You Died! All pending rewards are lost by 50%");

        float total = toastInTime + toastHoldTime + toastOutTime;
        yield return new WaitForSecondsRealtime(total);

        transitionLocked = false; // ✅ ÇOK ÖNEMLİ
        GoToIntermission();
    }

    private void CleanupAfterEliteFailOrDeath()
    {
        // Elite objesini sil
        if (currentEliteEnemy != null)
        {
            Destroy(currentEliteEnemy.gameObject);
            currentEliteEnemy = null;
        }

        // Elite state bayraklarını sıfırla
        eliteAlive = false;
        eliteTimer = 0f;

        // Sahayı temizle (opsiyonel ama çok iyi olur)
        spawner?.ClearAllEnemies();
        ClearAllWarnings();
        // ClearAllXpOrbs(); // istersen
    }
}
