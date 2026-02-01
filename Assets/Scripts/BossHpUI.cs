using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHpUI : MonoBehaviour
{
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private Slider hpSlider;

    private Enemy boundEnemy;

    public void Show(Enemy e, string displayName)
    {
        boundEnemy = e;
        gameObject.SetActive(true);

        if (bossNameText != null) bossNameText.text = displayName;

        Bind(e);

        // enemy ölürse UI kapanması için: basit çözüm -> null kontrol
        // (istersen Enemy.OnDeath event'i de ekleriz sonra)
    }

    public void Hide()
    {
        Unbind();
        gameObject.SetActive(false);
    }

    void Bind(Enemy e)
    {
        Unbind();

        if (e == null) return;

        e.OnHealthChanged += HandleHealthChanged;

        // ilk değer
        HandleHealthChanged(e.CurrentHp, e.CurrentMaxHp);
    }

    void Unbind()
    {
        if (boundEnemy != null)
            boundEnemy.OnHealthChanged -= HandleHealthChanged;

        boundEnemy = null;
    }

    void HandleHealthChanged(int hp, int maxHp)
    {
        if (hpSlider == null) return;
        hpSlider.maxValue = Mathf.Max(1, maxHp);
        hpSlider.value = Mathf.Clamp(hp, 0, maxHp);

        if (hp <= 0)
            Hide();
    }
}
