using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    GameManager gameManager;
    private List<GameObject> aliveEnemies = new List<GameObject>();

    [Header("Map Spawn Area")]
    [SerializeField] private Collider2D spawnArea;     // SpawnArea collider
    [SerializeField] private Transform playerTransform; // Player
    [SerializeField] private float minDistanceFromPlayer = 6f;
    [SerializeField] private int maxPositionTries = 25;

    [Header("Spawn Telegraph")]
    [SerializeField] private GameObject spawnWarningPrefab;
    [SerializeField] private float telegraphDelay = 1f;
    [SerializeField] private float cancelDistance = 2.5f; // oyuncu bu kadar yakındaysa spawn iptal


    [SerializeField] GameObject enemyPrefab;
    [SerializeField] GameObject enemyTankPrefab;
    [SerializeField] GameObject enemyElitePrefab;
    [SerializeField] GameObject enemyBossPrefab;

    [Header("Spawn")]
    [SerializeField] float spawnInterval = 1f;
    [SerializeField] int maxAliveEnemies = 50;
    private int currentWaveLevel = 1;
    private bool bossSpawned = false;


    float spawnTimer;
    public bool spawningEnabled = true;

    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        if (playerTransform == null)
        {
            var p = FindFirstObjectByType<Player>();
            if (p != null) playerTransform = p.transform;
        }
    }


    void Update()
    {
        if (!spawningEnabled) return;

        if (enemyPrefab == null) return;

        // Zamanları ilerlet
        float dt = Time.deltaTime;
        spawnTimer += dt;

        // Max enemy kontrolü
        if (GameObject.FindGameObjectsWithTag("Enemy").Length >= maxAliveEnemies)
            return;

        // Boss wave ise 1 kere boss spawnla
        if (IsBossWave(currentWaveLevel) && !bossSpawned && enemyBossPrefab != null)
        {
            // Boss'u map alanından bir noktaya telegraph ile spawnlayalım
            if (TryGetRandomPointInArea(out Vector3 bossPos))
            {
                bossSpawned = true;
                StartCoroutine(SpawnBossWithTelegraph(bossPos));
            }
        }


        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            TrySpawnFromMapArea();

        }
    }

    IEnumerator SpawnBossWithTelegraph(Vector3 pos)
    {
        GameObject warning = null;
        if (spawnWarningPrefab != null)
            warning = Instantiate(spawnWarningPrefab, pos, Quaternion.identity);

        float t = 0f;
        while (t < telegraphDelay)
        {
            t += Time.deltaTime;

            if (playerTransform != null && Vector2.Distance(playerTransform.position, pos) < cancelDistance)
            {
                // boss'u iptal etmeyelim, sadece başka yere atalım
                // bu en basit hali: yeni pozisyon seçip devam et
                if (TryGetRandomPointInArea(out Vector3 newPos))
                    pos = newPos;
            }

            yield return null;
        }

        if (warning != null) Destroy(warning);

        Instantiate(enemyBossPrefab, pos, Quaternion.identity);
    }


    void TrySpawnFromMapArea()
    {
        if (!TryGetRandomPointInArea(out Vector3 pos)) return;
        StartCoroutine(SpawnWithTelegraph(pos));
    }

    IEnumerator SpawnWithTelegraph(Vector3 pos)
    {
        GameObject warning = null;
        if (spawnWarningPrefab != null)
            warning = Instantiate(spawnWarningPrefab, pos, Quaternion.identity);

        float t = 0f;
        while (t < telegraphDelay)
        {
            t += Time.deltaTime;

            // Oyuncu spawn noktasına çok yaklaştıysa iptal
            if (playerTransform != null)
            {
                if (Vector2.Distance(playerTransform.position, pos) < cancelDistance)
                {
                    if (warning != null) Destroy(warning);
                    yield break;
                }
            }

            yield return null;
        }

        if (warning != null) Destroy(warning);

        // En son güvenlik: oyuncu hala üstündeyse spawn etme
        if (playerTransform != null && Vector2.Distance(playerTransform.position, pos) < cancelDistance)
            yield break;

        SpawnEnemy(pos);
    }


    bool TryGetRandomPointInArea(out Vector3 point)
    {
        point = Vector3.zero;
        if (spawnArea == null || playerTransform == null) return false;

        Bounds b = spawnArea.bounds;

        for (int i = 0; i < maxPositionTries; i++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float y = Random.Range(b.min.y, b.max.y);

            Vector3 candidate = new Vector3(x, y, 0f);

            if (!spawnArea.OverlapPoint(candidate))
                continue;

            if (Vector2.Distance(candidate, playerTransform.position) < minDistanceFromPlayer)
                continue;

            point = candidate;
            return true;
        }

        return false;
    }


    public void ApplyWaveSettings(int waveLevel)
    {
        currentWaveLevel = waveLevel;
        bossSpawned = false;

        // Spawn daha sık (0.2 altına düşmesin)
        spawnInterval = Mathf.Max(0.2f, 1f - (waveLevel - 1) * 0.1f);

        // Ekrandaki düşman limiti artsın
        maxAliveEnemies = 50 + (waveLevel - 1) * 10;
        Debug.Log($"Wave {waveLevel} settings applied");
    }

    public void ResetSpawnerSettings()
    {
        spawnTimer = 0f;

        ApplyWaveSettings(gameManager.currentWaveLevel);
    }

    // void SpawnAtScreenEdge()
    // {
    //     Camera cam = Camera.main;
    //     if (cam == null) return;

    //     int side = Random.Range(0, 4);

    //     Vector3 viewportPos;
    //     switch (side)
    //     {
    //         case 0: viewportPos = new Vector3(Random.value, 1f + outsideOffset, 0f); break; // üst
    //         case 1: viewportPos = new Vector3(Random.value, 0f - outsideOffset, 0f); break; // alt
    //         case 2: viewportPos = new Vector3(1f + outsideOffset, Random.value, 0f); break; // sağ
    //         default: viewportPos = new Vector3(0f - outsideOffset, Random.value, 0f); break; // sol
    //     }

    //     Vector3 world = cam.ViewportToWorldPoint(new Vector3(viewportPos.x, viewportPos.y, 0f));
    //     world.z = 0f;

    //     SpawnEnemy(world);
    // }

    float GetTankSpawnChance(float levelMultiplier)
    {
        float baseChance = 0.1f + (levelMultiplier - 1f) * 0.05f; // Level arttıkça temel şans artar
        return Mathf.Clamp01(baseChance);
    }

    float GetEliteSpawnChance(int waveLevel)
    {
        // Wave 5'ten önce elite yok
        if (waveLevel < 5) return 0f;

        // 5'te %5, sonra yavaş artsın
        float c = 0.05f + (waveLevel - 5) * 0.01f;
        return Mathf.Clamp01(c);
    }

    bool IsBossWave(int waveLevel)
    {
        return waveLevel == 10;
    }

    void SpawnEnemy(Vector3 position)
    {
        // Wave 5+ : elite havuza girer
        float eliteChance = GetEliteSpawnChance(currentWaveLevel);

        // Tank şansı: senin mevcut sistemin
        float levelMultiplier = 1f + (currentWaveLevel - 1) * 0.10f;
        float tankChance = GetTankSpawnChance(levelMultiplier);

        // 1) Elite roll (önce)
        bool spawnElite = (enemyElitePrefab != null) && (Random.value < eliteChance);

        // 2) Elite değilse Tank roll
        bool spawnTank = (!spawnElite) && (enemyTankPrefab != null) && (Random.value < tankChance);

        // 3) Prefab seç
        GameObject prefab = enemyPrefab;
        if (spawnElite) prefab = enemyElitePrefab;
        else if (spawnTank) prefab = enemyTankPrefab;

        // Güvenlik
        if (prefab == null) return;

        var enemy = Instantiate(prefab, position, Quaternion.identity);
        aliveEnemies.Add(enemy);
    }


    // void SpawnEnemy(Vector3 position)
    // {
    //     // Level çarpanı: currentLevel'ı direkt kullanma, yumuşat
    //     // L1=1.0, L2=1.1, L3=1.2, ...
    //     float levelMultiplier = 1f + (gameManager.currentWaveLevel - 1) * 0.10f;

    //     float chance = GetTankSpawnChance(levelMultiplier);

    //     bool spawnTank = (enemyTankPrefab != null) && (Random.value < chance);

    //     var enemy = Instantiate(spawnTank ? enemyTankPrefab : enemyPrefab, position, Quaternion.identity);
    //     aliveEnemies.Add(enemy);
    // }

    public void ClearAllEnemies()
    {
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Destroy(enemy);
        }
        aliveEnemies.Clear();
    }
}
