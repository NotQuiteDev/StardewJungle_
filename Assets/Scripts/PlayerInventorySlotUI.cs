using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInventorySlotUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button sellAllButton; // ## 추가: '모두 판매' 버튼 ##

    private ItemData currentItemData;
    private int currentItemQuantity;

    private void Awake()
    {
        if (sellButton != null)
        {
            sellButton.onClick.AddListener(OnSellButtonClick);
        }
        // ## 추가: '모두 판매' 버튼에 리스너(클릭 이벤트) 연결 ##
        if (sellAllButton != null)
        {
            sellAllButton.onClick.AddListener(OnSellAllButtonClick);
        }
    }

    /// <summary>
    /// 이 슬롯에 플레이어의 아이템 정보를 채워넣는 함수
    /// </summary>
    public void Setup(ItemData itemToDisplay, int quantity)
    {
        currentItemData = itemToDisplay;
        currentItemQuantity = quantity;

        if (currentItemData == null || currentItemQuantity <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        itemIcon.sprite = currentItemData.itemIcon;
        itemNameText.text = currentItemData.itemName;
        infoText.text = $"$ {currentItemData.sellPrice:N0} (x{currentItemQuantity})";
        
        // '판매' 버튼은 항상 활성화
        if(sellButton != null) sellButton.interactable = true;
        
        // ## 추가: 아이템 수량이 2개 이상일 때만 '모두 판매' 버튼을 활성화 ##
        // (1개일 때는 '판매'와 기능이 같으므로 비활성화하여 혼동을 방지)
        if(sellAllButton != null) sellAllButton.interactable = (currentItemQuantity > 1);
        
        gameObject.SetActive(true);
    }

    /// <summary>
    /// '판매' 버튼이 눌렸을 때 호출될 함수 (1개 판매)
    /// </summary>
    public void OnSellButtonClick()
    {
        if (currentItemData != null)
        {
            ShopUIManager.Instance.TrySellItem(currentItemData, 1);
        }
    }

    /// <summary>
    /// ## 추가: '모두 판매' 버튼이 눌렸을 때 호출될 함수 ##
    /// </summary>
    public void OnSellAllButtonClick()
    {
        if (currentItemData != null && currentItemQuantity > 0)
        {
            // 현재 슬롯에 있는 아이템 전체 수량을 판매하도록 요청
            ShopUIManager.Instance.TrySellItem(currentItemData, currentItemQuantity);
        }
    }
}