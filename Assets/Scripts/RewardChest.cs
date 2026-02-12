using System.Collections;
using UnityEngine;

public class RewardChest : MonoBehaviour
{
    [Header("Chest Anim")]
    [SerializeField] private Animator chestAnimator;        // ChestSprite animator
    [SerializeField] private string openTrigger = "Open";

    private bool notified;
    private Collider2D col;

    [Header("Click")]
    [SerializeField] private LayerMask interactableMask;

    [Header("Rarity FX (frames)")]
    [SerializeField] private SpriteRenderer fxRenderer;     // FX child
    [SerializeField] private float fxFrameRate = 18f;

    [SerializeField] private Sprite[] commonFxFrames;
    [SerializeField] private Sprite[] uncommonFxFrames;
    [SerializeField] private Sprite[] rareFxFrames;
    [SerializeField] private Sprite[] epicFxFrames;
    [SerializeField] private Sprite[] legendaryFxFrames;

    [SerializeField] private float suspenseDelayAfterOpen = 0.25f; // açıldıktan sonra mini bekleme
    [SerializeField] private float suspenseDelayAfterFx = 0.30f;   // fx bitince ekstra bekleme


    private bool opened;
    private GameManager gm;

    // GM chest spawn ederken set edecek
    private int rarityIndex = 0; // 0 common, 1 rare, 2 epic, 3 legendary

    public void Init(GameManager gameManager) => gm = gameManager;

    public void SetRarityIndex(int idx) => rarityIndex = idx;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (opened) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, interactableMask);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
                TryOpen();
        }
    }

    public void TryOpen()
    {
        if (opened) return;
        opened = true;

        if (col != null) col.enabled = false; // ✅ tekrar tıklanmasın
        chestAnimator?.SetTrigger(openTrigger);
    }


    // Animation Event: Chest_Open clipinin sonunda çağır
    public void NotifyOpened()
    {
        if (notified) return;   // ✅ event yanlışlıkla 2 kez gelirse de çalışmaz
        notified = true;

        StartCoroutine(Co_OpenSequence());
    }


    private IEnumerator Co_OpenSequence()
    {
        // 1) chest açıldı -> mini “acaba?” duraksaması
        yield return new WaitForSecondsRealtime(suspenseDelayAfterOpen);

        // 2) FX oynat
        var frames = GetFramesForRarity();
        if (fxRenderer != null && frames != null && frames.Length > 0)
            yield return StartCoroutine(PlayFxFrames(frames));

        // 3) FX bittikten sonra kısa wow-pause
        yield return new WaitForSecondsRealtime(suspenseDelayAfterFx);

        // 4) sonra panel
        gm?.OnChestOpened();
    }



    private Sprite[] GetFramesForRarity()
    {
        return rarityIndex switch
        {
            0 => commonFxFrames,
            1 => uncommonFxFrames,
            2 => rareFxFrames,
            3 => epicFxFrames,
            4 => legendaryFxFrames,
            _ => commonFxFrames
        };
    }

    private IEnumerator PlayFxFrames(Sprite[] frames)
    {
        fxRenderer.gameObject.SetActive(true);

        float secPerFrame = 1f / Mathf.Max(1f, fxFrameRate);

        for (int i = 0; i < frames.Length; i++)
        {
            fxRenderer.sprite = frames[i];
            yield return new WaitForSecondsRealtime(secPerFrame);
        }

        fxRenderer.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // Spawn olduğunda temiz başlangıç
        opened = false;
        notified = false;

        if (col != null) col.enabled = true;

        // FX her zaman kapalı başlasın (spawn bug'ını bu çözer)
        if (fxRenderer != null)
        {
            fxRenderer.sprite = null;
            fxRenderer.gameObject.SetActive(false);
        }

        // Animator state'i de temizle (bazen spawn'da tuhaf state ile gelir)
        if (chestAnimator != null)
        {
            chestAnimator.Rebind();
            chestAnimator.Update(0f);
        }
    }
}
