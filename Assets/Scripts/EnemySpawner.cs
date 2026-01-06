using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    GameManager gameManager;
    private List<GameObject> aliveEnemies = new List<GameObject>();

    [SerializeField] GameObject enemyPrefab;
    [SerializeField] GameObject enemyTankPrefab;

    [Header("Spawn")]
    [SerializeField] float spawnInterval = 1f;
    [SerializeField] int maxAliveEnemies = 50;
    [SerializeField] float outsideOffset = 0.05f;

    float spawnTimer;
    public bool spawningEnabled = true;

    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
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

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnAtScreenEdge();
        }
    }

    public void ApplyWaveSettings(int waveLevel)
    {
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

    void SpawnAtScreenEdge()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        int side = Random.Range(0, 4);

        Vector3 viewportPos;
        switch (side)
        {
            case 0: viewportPos = new Vector3(Random.value, 1f + outsideOffset, 0f); break; // üst
            case 1: viewportPos = new Vector3(Random.value, 0f - outsideOffset, 0f); break; // alt
            case 2: viewportPos = new Vector3(1f + outsideOffset, Random.value, 0f); break; // sağ
            default: viewportPos = new Vector3(0f - outsideOffset, Random.value, 0f); break; // sol
        }

        Vector3 world = cam.ViewportToWorldPoint(new Vector3(viewportPos.x, viewportPos.y, 0f));
        world.z = 0f;

        SpawnEnemy(world);
    }
    
    float GetTankSpawnChance(float levelMultiplier)
    {
        float baseChance = 0.1f + (levelMultiplier - 1f) * 0.05f; // Level arttıkça temel şans artar
        return Mathf.Clamp01(baseChance);
    }

    void SpawnEnemy(Vector3 position)
    {
        // Level çarpanı: currentLevel'ı direkt kullanma, yumuşat
        // L1=1.0, L2=1.1, L3=1.2, ...
        float levelMultiplier = 1f + (gameManager.currentWaveLevel - 1) * 0.10f;

        float chance = GetTankSpawnChance(levelMultiplier);

        bool spawnTank = (enemyTankPrefab != null) && (Random.value < chance);

        var enemy = Instantiate(spawnTank ? enemyTankPrefab : enemyPrefab, position, Quaternion.identity);
        aliveEnemies.Add(enemy);
    }

    public void ClearAllEnemies()
    {
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Destroy(enemy);
        }
        aliveEnemies.Clear();
    }
}
