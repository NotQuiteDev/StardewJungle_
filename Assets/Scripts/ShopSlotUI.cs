using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button buyAllButton;

    private ShopItem currentItem;

    private void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyButtonClick);
        }
        if (buyAllButton != null)
        {
            buyAllButton.onClick.AddListener(OnBuyAllButtonClick);
        }
    }

    public void Setup(ShopItem itemToDisplay)
    {
        currentItem = itemToDisplay;
        if (currentItem == null || currentItem.item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        itemIcon.sprite = currentItem.item.itemIcon;
        itemNameText.text = currentItem.item.itemName;
        priceText.text = $"$ {currentItem.price:N0}";

        bool canAffordOne = ShopUIManager.Instance.CanAfford(currentItem.price);
        if(buyButton != null) buyButton.interactable = canAffordOne;
        if(buyAllButton != null) buyAllButton.interactable = canAffordOne;

        gameObject.SetActive(true);
    }

    public void OnBuyButtonClick()
    {
        if (currentItem != null)
        {
            // ## 수정: 1개 구매 시도 후, 성공했다면 UI를 새로고침 ##
            if(ShopUIManager.Instance.TryPurchaseItem(currentItem))
            {
                ShopUIManager.Instance.RefreshUI(); // public으로 변경 필요
            }
        }
    }

    public void OnBuyAllButtonClick()
    {
        if (currentItem != null)
        {
            ShopUIManager.Instance.TryPurchaseMaxAffordable(currentItem);
        }
    }
}