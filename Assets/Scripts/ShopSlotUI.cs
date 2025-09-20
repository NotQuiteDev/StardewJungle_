using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI infoText; // 가격 또는 수량을 표시할 텍스트
    [SerializeField] private Button slotButton;

    // ## 추가: 슬롯의 두 가지 모드 정의 ##
    public enum SlotMode { Buy, Sell }
    private SlotMode currentMode;

    // 슬롯이 현재 대표하는 아이템 정보들
    private ShopItem currentShopItem;   // 구매용 정보 (아이템 + 구매가)
    private ItemData currentItemData;   // 판매용 정보 (아이템)
    private int currentItemQuantity;  // 판매용 정보 (수량)

    private void Awake()
    {
        // 버튼이 눌렸을 때 OnSlotButtonClick 함수가 호출되도록 연결
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotButtonClick);
        }
    }

    /// <summary>
    /// '구매' 목록에 표시될 슬롯을 설정합니다.
    /// </summary>
    public void SetupForBuy(ShopItem itemToDisplay)
    {
        currentMode = SlotMode.Buy;
        currentShopItem = itemToDisplay;
        currentItemData = itemToDisplay.item;

        if (currentItemData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // UI 요소 업데이트
        itemIcon.sprite = currentItemData.itemIcon;
        itemNameText.text = currentItemData.itemName;
        infoText.text = $"₩ {currentShopItem.price:N0}"; // '구매 가격' 표시

        gameObject.SetActive(true);
    }

    /// <summary>
    /// '판매' 목록에 표시될 슬롯을 설정합니다.
    /// </summary>
    public void SetupForSell(ItemData itemToDisplay, int quantity)
    {
        currentMode = SlotMode.Sell;
        currentItemData = itemToDisplay;
        currentItemQuantity = quantity;

        if (currentItemData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // UI 요소 업데이트
        itemIcon.sprite = currentItemData.itemIcon;
        itemNameText.text = currentItemData.itemName;

        // 판매 가격이 0 이하면 '판매 불가', 아니면 '판매 가격'과 '수량' 표시
        if (currentItemData.sellPrice <= 0)
        {
            infoText.text = "판매 불가";
        }
        else
        {
            infoText.text = $"₩ {currentItemData.sellPrice:N0} (x{currentItemQuantity})";
        }
        
        gameObject.SetActive(true);
    }


    /// <summary>
    /// 이 슬롯의 버튼이 눌렸을 때 호출될 함수
    /// </summary>
    public void OnSlotButtonClick()
    {
        // TODO: 여기서 바로 구매/판매를 하는 대신, '거래 확정 패널'을 띄워주는 로직으로 발전시킬 거야.
        // 지금은 기능 테스트를 위해 바로 실행되도록 만들자.

        switch (currentMode)
        {
            case SlotMode.Buy:
                if (currentShopItem != null)
                {
                    Debug.Log($"{currentShopItem.item.itemName} 구매 시도.");
                    ShopUIManager.Instance.TryPurchaseItem(currentShopItem);
                }
                break;

            case SlotMode.Sell:
                if (currentItemData != null)
                {
                    Debug.Log($"{currentItemData.itemName} 판매 시도.");
                    // ShopUIManager에 판매 함수를 추가하고 호출해야 함 (다음 단계)
                    // ShopUIManager.Instance.TrySellItem(currentItemData, 1);
                }
                break;
        }
    }
}