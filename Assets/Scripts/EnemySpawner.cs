using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] GameObject enemyPrefab;
    [SerializeField] GameObject enemyTankPrefab;
    [SerializeField] float spawnInterval = 1f;
    [SerializeField] int maxAliveEnemies = 50;
    [SerializeField] float outsideOffset = 0.05f;

    private float elapsedTime;
    private float timer;

    private void Update()
    {
        Debug.Log(elapsedTime);
        if (enemyPrefab == null) return;

        if (GameObject.FindGameObjectsWithTag("Enemy").Length >= maxAliveEnemies) { return; }

        elapsedTime = Time.timeSinceLevelLoad;
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnAtScreenEdge();
        }
    }

    private void SpawnAtScreenEdge()
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

    private float GetTankSpawnChance()
    {
        if (elapsedTime < 20)
        {
            return 0;
        }
        else if (elapsedTime >= 20 && elapsedTime < 40)
        {
            return 0.05f;
        }
        else if (elapsedTime >= 40 && elapsedTime < 60)
        {
            return 0.10f;
        }
        else
        {
            return 0.15f;
        }
    }

    private void SpawnEnemy(Vector3 position)
    {
        float chance = GetTankSpawnChance();
        if (Random.value < chance)
        {
            if (enemyTankPrefab == null) { return; }
            Instantiate(enemyTankPrefab, position, Quaternion.identity);
        }
        else
        {
            Instantiate(enemyPrefab, position, Quaternion.identity);
        }
    }
}
