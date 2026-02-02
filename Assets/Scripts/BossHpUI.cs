using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossHpUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private CanvasGroup group;


    [Header("Name Animation")]
    [SerializeField] private float letterDelay = 0.04f;
    [SerializeField] private float popScale = 1.15f;
    [SerializeField] private float popDuration = 0.15f;

    private Enemy boundEnemy;
    private Coroutine nameRoutine;
    private Vector3 baseScale;

    private void Awake()
    {
        if (bossNameText != null)
            baseScale = bossNameText.transform.localScale;

        if (group == null) group = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    private void SetVisible(bool v)
    {
        if (group == null) return;
        group.alpha = v ? 1f : 0f;
        group.interactable = v;
        group.blocksRaycasts = v;
    }

    public void Show(Enemy e, string displayName)
    {
        SetVisible(true);
        Bind(e);

        if (nameRoutine != null)
            StopCoroutine(nameRoutine);

        nameRoutine = StartCoroutine(PlayNameAnimation(displayName));
    }

    private IEnumerator ShowNextFrame(string displayName)
    {
        // 1 frame bekle (aktiflik kesinleşsin)
        yield return null;

        nameRoutine = StartCoroutine(PlayNameAnimation(displayName));
    }


    public void Hide()
    {
        Unbind();
        SetVisible(false);
    }

    // ---------------- HP ----------------

    void Bind(Enemy e)
    {
        Unbind();
        boundEnemy = e;

        if (e == null) return;

        e.OnHealthChanged += HandleHealthChanged;
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

    // ---------------- NAME ANIMATION ----------------

    IEnumerator PlayNameAnimation(string fullName)
    {
        bossNameText.text = "";
        bossNameText.transform.localScale = baseScale;

        // Harf harf yaz
        for (int i = 0; i < fullName.Length; i++)
        {
            bossNameText.text += fullName[i];
            yield return new WaitForSecondsRealtime(letterDelay);
        }

        // Küçük pop efekti
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = t / popDuration;
            bossNameText.transform.localScale =
                Vector3.Lerp(baseScale, baseScale * popScale, a);
            yield return null;
        }

        t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = t / popDuration;
            bossNameText.transform.localScale =
                Vector3.Lerp(baseScale * popScale, baseScale, a);
            yield return null;
        }

        bossNameText.transform.localScale = baseScale;
    }
}
