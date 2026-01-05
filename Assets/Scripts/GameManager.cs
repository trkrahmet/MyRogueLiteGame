using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveDuration = 60f;
    public int currentWave = 1;
    float waveTimer;
    bool inCombat = true;

    [Header("References")]
    public EnemySpawner spawner;
    public Player player;

    [Header("UI (TMP)")]
    public TMP_Text waveText;
    public TMP_Text timerText;
    public TMP_Text levelText;
    public GameObject upgradePanel;

    private void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (player == null) player = FindFirstObjectByType<Player>();

        waveTimer = waveDuration;
        if (upgradePanel != null) { upgradePanel.SetActive(false); }

        StartCombat();
        UpdateUI();
    }

    private void Update()
    {
        if (!inCombat)
        {
            if (Input.GetKeyDown(KeyCode.Return))
                ContinueAfterUpgrade();
            return;
        }

        waveTimer -= Time.deltaTime;
        if (waveTimer <= 0f)
        {
            EndCombat();
            return;
        }
        UpdateUI();
    }

    private void StartCombat()
    {
        inCombat = true;
        waveTimer = waveDuration;

        if (spawner != null) { spawner.spawningEnabled = true; }

        if (upgradePanel != null) { upgradePanel.SetActive(false); }

        Time.timeScale = 1f;
    }

    private void EndCombat()
    {
        inCombat = false;

        if (spawner != null) { spawner.spawningEnabled = false; }
        if (upgradePanel != null) { upgradePanel.SetActive(true); }

        Time.timeScale = 0f;

        UpdateUI();
        Debug.Log($"Wave {currentWave} ended. Prepare for upgrades!");
    }

    private void UpdateUI()
    {
        if (waveText != null) { waveText.text = $"Wave: {currentWave}"; }
        if (timerText != null) timerText.text = $"Time: {Mathf.CeilToInt(waveTimer)}";
        if (levelText != null && spawner != null) { levelText.text = $"Level: {spawner.currentLevel}"; }
    }

    public void ContinueAfterUpgrade()
    {
        currentWave++;
        spawner.ApplyWaveSettings(currentWave);
        StartCombat();
    }
}
