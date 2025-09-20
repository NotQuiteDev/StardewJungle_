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

    private ShopItem currentItem;

    private void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyButtonClick);
        }
    }

    /// <summary>
    /// '구매' 목록에 표시될 슬롯을 설정합니다.
    /// </summary>
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

        gameObject.SetActive(true);
    }

    /// <summary>
    /// '구매' 버튼이 눌렸을 때 호출될 함수
    /// </summary>
    public void OnBuyButtonClick()
    {
        if (currentItem != null)
        {
            ShopUIManager.Instance.TryPurchaseItem(currentItem);
        }
    }
}