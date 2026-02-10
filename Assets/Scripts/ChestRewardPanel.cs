using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChestRewardPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image bg;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text desc;

    [SerializeField] private Button takeButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private TMP_Text sellText;
    [SerializeField] private TMP_Text takeHintText; // opsiyonel: "No slot" vb.

    public void Show(
        ShopPanel.ChestOfferData offer,
        Color rarityColor,
        int sellValue,
        bool canTake,
        Action onTake,
        Action onSell)
    {
        gameObject.SetActive(true);

        if (bg != null) bg.color = rarityColor;
        if (icon != null) icon.sprite = offer.icon;
        if (title != null) title.text = offer.title;
        if (desc != null) desc.text = offer.desc;

        if (sellText != null) sellText.text = $"Sat (+{sellValue})";

        takeButton.onClick.RemoveAllListeners();
        sellButton.onClick.RemoveAllListeners();

        takeButton.interactable = canTake;
        if (takeHintText != null)
            takeHintText.text = canTake ? "" : "Boş silah slotu yok!";

        takeButton.onClick.AddListener(() => onTake?.Invoke());
        sellButton.onClick.AddListener(() => onSell?.Invoke());
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
