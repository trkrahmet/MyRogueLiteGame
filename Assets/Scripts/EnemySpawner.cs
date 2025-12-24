using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] GameObject enemyPrefab;
    [SerializeField] GameObject enemyTankPrefab;

    [Header("Spawn")]
    [SerializeField] float spawnInterval = 1f;
    [SerializeField] int maxAliveEnemies = 50;
    [SerializeField] float outsideOffset = 0.05f;

    [Header("Level")]
    public int currentLevel = 1;
    [SerializeField] float levelDuration = 30f;

    float spawnTimer;
    float levelTimer;

    void Update()
    {
        if (enemyPrefab == null) return;

        // Zamanları ilerlet
        float dt = Time.deltaTime;
        levelTimer += dt;
        spawnTimer += dt;

        // Önce level güncelle (interval/cap değişsin), sonra spawn kontrol et
        UpdateLevel();

        // Max enemy kontrolü
        if (GameObject.FindGameObjectsWithTag("Enemy").Length >= maxAliveEnemies)
            return;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnAtScreenEdge();
        }
    }

    void UpdateLevel()
    {
        if (levelTimer < levelDuration) return;

        currentLevel++;
        levelTimer = 0f;

        ApplyLevelSettings(currentLevel);

        // İstersen test için:
        Debug.Log($"LEVEL UP -> {currentLevel} | interval:{spawnInterval} | cap:{maxAliveEnemies}");
    }

    void ApplyLevelSettings(int level)
    {
        // Spawn daha sık (0.2 altına düşmesin)
        spawnInterval = Mathf.Max(0.2f, 1f - (level - 1) * 0.1f);

        // Ekrandaki düşman limiti artsın
        maxAliveEnemies = 50 + (level - 1) * 10;
    }

    public void ResetSpawnerSettings()
    {
        currentLevel = 1;
        levelTimer = 0f;
        spawnTimer = 0f;

        ApplyLevelSettings(currentLevel);
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

    // Tank "fazı": level içindeki zamana göre (levelTimer)
    float GetTankSpawnChance(float levelElapsedTime, float levelMultiplier)
    {
        float baseChance;

        if (levelElapsedTime < 10f) baseChance = 0.01f; // levelin başı: çok az
        else if (levelElapsedTime < 20f) baseChance = 0.05f; // orta: biraz
        else baseChance = 0.10f; // son: daha çok

        // Yumuşak çarpanla güçlendir
        float chance = baseChance * levelMultiplier;

        // Güvenlik: 0..0.90 arası tut (1 olmasın, yoksa hep tank olur)
        return Mathf.Clamp(chance, 0f, 0.90f);
    }

    void SpawnEnemy(Vector3 position)
    {
        // Level çarpanı: currentLevel'ı direkt kullanma, yumuşat
        // L1=1.0, L2=1.1, L3=1.2, ...
        float levelMultiplier = 1f + (currentLevel - 1) * 0.10f;

        float chance = GetTankSpawnChance(levelTimer, levelMultiplier);

        bool spawnTank = (enemyTankPrefab != null) && (Random.value < chance);

        Instantiate(spawnTank ? enemyTankPrefab : enemyPrefab, position, Quaternion.identity);
    }
}
