using UnityEngine;

public class RewardChest : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    private bool opened = false;
    private GameManager gm;

    public void Init(GameManager gameManager)
    {
        gm = gameManager;
    }

    private void OnMouseDown()
    {
        // MouseDown touch ile de çalışır (mobilde pratik)
        TryOpen();
    }

    public void TryOpen()
    {
        if (opened) return;
        opened = true;

        if (animator != null)
            animator.SetTrigger(openTrigger);

        // anim süresi kadar bekleyip gm'ye haber verebilirsin
        // şimdilik direkt geçiyoruz:
        gm?.OnChestOpened();
    }
}
