using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInventorySlotUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI infoText; // 판매 가격과 수량을 표시
    [SerializeField] private Button sellButton;

    private ItemData currentItemData;
    private int currentItemQuantity;

    /// <summary>
    /// 이 슬롯에 플레이어의 아이템 정보를 채워넣는 함수
    /// </summary>
    public void Setup(ItemData itemToDisplay, int quantity)
    {
        currentItemData = itemToDisplay;
        currentItemQuantity = quantity;

        if (currentItemData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        itemIcon.sprite = currentItemData.itemIcon;
        itemNameText.text = currentItemData.itemName;

        // ## 수정: 텍스트를 영어와 $ 기호로 변경 ##
        infoText.text = $"$ {currentItemData.sellPrice:N0} (x{currentItemQuantity})";
        if(sellButton != null) sellButton.interactable = true;
        
        gameObject.SetActive(true);
    }

    /// <summary>
    /// '판매' 버튼이 눌렸을 때 호출될 함수
    /// </summary>
    public void OnSellButtonClick()
    {
        if (currentItemData != null)
        {
            // 실제 판매 처리는 ShopUIManager에게 맡긴다. (1개씩 판매)
            ShopUIManager.Instance.TrySellItem(currentItemData, 1);
        }
    }
    private void Awake()
    {
        // 이 스크립트에는 Awake가 없었으니 새로 추가한다.
        if (sellButton != null)
        {
            sellButton.onClick.AddListener(OnSellButtonClick);
        }
    }
}