using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // Image, Button 등 UI 컴포넌트 사용

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    [Header("UI 연결")]
    [SerializeField] private GameObject shopWindowObject; // 상점 UI 전체 창
    [SerializeField] private Image npcPortraitImage;      // NPC 초상화 이미지
    [SerializeField] private Transform buyPanelContent;   // 구매 패널의 Content
    [SerializeField] private Transform sellPanelContent;  // 판매 패널의 Content
    [SerializeField] private GameObject buySlotPrefab;  // '구매용 카드' 프리팹
    [SerializeField] private GameObject sellSlotPrefab; // '판매용 카드' 프리팹
    [SerializeField] private Button closeButton;

    private InteractableNPC currentMerchant;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        // 닫기 버튼에 기능 연결
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseShop);
        }
        // 시작할 땐 무조건 꺼둔다
        shopWindowObject.SetActive(false);
    }

    public void OpenShop(InteractableNPC merchant)
    {
        GameManager.Instance.EnterUIMode();
        currentMerchant = merchant;
        shopWindowObject.SetActive(true);

        // 상인 정보로 UI 업데이트
        npcPortraitImage.sprite = currentMerchant.npcPortrait;

        // 패널 채우기
        PopulateBuyPanel(currentMerchant.shopItemList);
        PopulateSellPanel();
    }

    public void CloseShop()
    {
        GameManager.Instance.EnterGameplayMode();
        currentMerchant = null;
        shopWindowObject.SetActive(false);
    }

    // 구매 패널을 상인의 판매 목록으로 채우는 함수
    private void PopulateBuyPanel(List<ShopItem> items)
    {
        foreach (Transform child in buyPanelContent) { Destroy(child.gameObject); }

        foreach (var item in items)
        {
            // ## 수정: '구매용' 프리팹 사용 ##
            GameObject slotInstance = Instantiate(buySlotPrefab, buyPanelContent);
            ShopSlotUI slotUI = slotInstance.GetComponent<ShopSlotUI>();
            slotUI.Setup(item);
        }
    }

    // 판매 패널을 플레이어의 인벤토리로 채우는 함수
    private void PopulateSellPanel()
    {
        foreach (Transform child in sellPanelContent) { Destroy(child.gameObject); }

        var inventoryData = InventoryManager.Instance.inventorySlotsData;
        var inventoryCounts = InventoryManager.Instance.slotCounts;

        for (int i = 0; i < inventoryData.Length; i++)
        {
            if (inventoryData[i] != null)
            {
                // ## 수정: '판매용' 프리팹 사용 ##
                GameObject slotInstance = Instantiate(sellSlotPrefab, sellPanelContent);
                PlayerInventorySlotUI slotUI = slotInstance.GetComponent<PlayerInventorySlotUI>();
                slotUI.Setup(inventoryData[i], inventoryCounts[i]);
            }
        }
    }

    /// <summary>
    /// 아이템 판매를 시도하는 함수 (판매용 카드가 호출)
    /// </summary>
    // ## 추가: 판매 기능 구현 ##
    public void TrySellItem(ItemData itemData, int quantity)
    {
        if (InventoryManager.Instance.RemoveItem(itemData, quantity))
        {
            int sellPrice = itemData.sellPrice * quantity;
            MoneyManager.Instance.AddMoney(sellPrice);
            Debug.Log($"Sold {itemData.itemName} x{quantity} for ${sellPrice}.");
            
            // ## 수정: 판매 성공 후, 판매 패널을 즉시 새로고침 ##
            PopulateSellPanel(); 
        }
        else
        {
            Debug.LogWarning($"Failed to sell {itemData.itemName}. (Not enough quantity?)");
        }
    }

    public void TryPurchaseItem(ShopItem shopItem)
    {
        if (shopItem == null) return;

        if (MoneyManager.Instance.SpendMoney(shopItem.price))
        {
            bool success = InventoryManager.Instance.AddItem(shopItem.item, 1);

            if (success)
            {
                Debug.Log($"Purchased {shopItem.item.itemName} successfully!");
                // ## 추가: 구매 성공 후, 판매 패널을 즉시 새로고침 ##
                // 내 인벤토리가 바뀌었으니 판매 목록도 갱신되어야 함
                PopulateSellPanel();
            }
            else
            {
                Debug.Log("Purchase failed. Inventory is full. Refunding money.");
                MoneyManager.Instance.AddMoney(shopItem.price);
            }
        }
        else
        {
            Debug.Log("Not enough money to purchase.");
        }
    }
}
