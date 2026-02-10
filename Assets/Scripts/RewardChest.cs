using System.Collections;
using UnityEngine;

public class RewardChest : MonoBehaviour
{
    [Header("Chest Anim")]
    [SerializeField] private Animator chestAnimator;        // ChestSprite animator
    [SerializeField] private string openTrigger = "Open";

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

        chestAnimator?.SetTrigger(openTrigger);
        // ❌ GM çağırma yok, anim bitince event gelecek
    }

    // Animation Event: Chest_Open clipinin sonunda çağır
    public void NotifyOpened()
    {
        StartCoroutine(Co_OpenSequence());
    }

    private IEnumerator Co_OpenSequence()
    {
        // Chest açıldıktan sonra minik “acaba?” duraksaması
        yield return new WaitForSecondsRealtime(suspenseDelayAfterOpen);

        // FX oynat
        var frames = GetFramesForRarity();
        if (fxRenderer != null && frames != null && frames.Length > 0)
            yield return StartCoroutine(PlayFxFrames(frames));

        // FX bittikten sonra “wow pause”
        yield return new WaitForSecondsRealtime(suspenseDelayAfterFx);

        // Sonra panel
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
}
