using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider hpSlider; // opsiyonel

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<Player>();
    }

    private void OnEnable()
    {
        if (player == null) player = FindFirstObjectByType<Player>();
        if (player != null)
            player.OnHealthChanged += HandleHealthChanged;

        // ilk açılışta da güncellemek için:
        ForceRefresh();
    }

    private void OnDisable()
    {
        if (player != null)
            player.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (hpText != null)
            hpText.text = $"HP: {current} / {max}";

        if (hpSlider != null)
        {
            hpSlider.maxValue = max;
            hpSlider.value = current;
        }
    }

    private void ForceRefresh()
    {
        if (player == null) return;
        HandleHealthChanged(player.CurrentHp, player.MaxHp);
    }

}
