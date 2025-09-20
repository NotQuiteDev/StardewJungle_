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
    [SerializeField] private GameObject shopSlotPrefab;   // '상품 카드' 프리팹
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
        currentMerchant = null;
        shopWindowObject.SetActive(false);
    }

    // 구매 패널을 상인의 판매 목록으로 채우는 함수
    private void PopulateBuyPanel(List<ShopItem> items)
    {
        // 기존에 있던 슬롯들 모두 삭제
        foreach (Transform child in buyPanelContent)
        {
            Destroy(child.gameObject);
        }

        // 새 목록으로 슬롯 생성
        foreach (var item in items)
        {
            GameObject slotInstance = Instantiate(shopSlotPrefab, buyPanelContent);
            ShopSlotUI slotUI = slotInstance.GetComponent<ShopSlotUI>();
            slotUI.SetupForBuy(item);
        }
    }

    // 판매 패널을 플레이어의 인벤토리로 채우는 함수
    private void PopulateSellPanel()
    {
        foreach (Transform child in sellPanelContent)
        {
            Destroy(child.gameObject);
        }
        
        // InventoryManager에서 플레이어 아이템 목록 가져오기
        var inventoryData = InventoryManager.Instance.inventorySlotsData;
        var inventoryCounts = InventoryManager.Instance.slotCounts;

        for (int i = 0; i < inventoryData.Length; i++)
        {
            if (inventoryData[i] != null)
            {
                GameObject slotInstance = Instantiate(shopSlotPrefab, sellPanelContent);
                ShopSlotUI slotUI = slotInstance.GetComponent<ShopSlotUI>();
                slotUI.SetupForSell(inventoryData[i], inventoryCounts[i]);
            }
        }
    }

    /// <summary>
    /// 아이템 구매를 시도하는 함수 (UI의 구매 버튼이 호출)
    /// </summary>
    public void TryPurchaseItem(ShopItem shopItem)
    {
        if (shopItem == null) return;

        // 1. 돈이 충분한가?
        if (MoneyManager.Instance.SpendMoney(shopItem.price))
        {
            // 2. 인벤토리에 공간이 있는가?
            // (InventoryManager.AddItem은 공간이 있으면 true, 없으면 false를 반환해야 함)
            bool success = InventoryManager.Instance.AddItem(shopItem.item, 1); // 일단 1개씩만 구매

            if (success)
            {
                Debug.Log($"{shopItem.item.itemName} 구매 성공!");
            }
            else
            {
                // 인벤토리가 꽉 찼으므로, 방금 썼던 돈을 돌려준다.
                Debug.Log("인벤토리가 가득 차서 구매에 실패했습니다. 돈을 환불합니다.");
                MoneyManager.Instance.AddMoney(shopItem.price);
            }
        }
        else
        {
            Debug.Log("돈이 부족하여 구매할 수 없습니다.");
        }
    }
    /// <summary>
    /// 아이템 판매를 시도하는 함수 (판매용 카드가 호출)
    /// </summary>
    public void TrySellItem(ItemData itemData, int quantity)
    {
        // 판매 불가능한 아이템인지 확인
        if (itemData.sellPrice <= 0)
        {
            Debug.Log($"{itemData.itemName}은(는) 판매할 수 없는 아이템입니다.");
            return;
        }

        // 인벤토리에서 아이템 제거 시도
        if (InventoryManager.Instance.RemoveItem(itemData, quantity))
        {
            // 성공하면 돈 받기
            int sellPrice = itemData.sellPrice * quantity;
            MoneyManager.Instance.AddMoney(sellPrice);
            Debug.Log($"{itemData.itemName} {quantity}개를 {sellPrice}원에 판매했습니다.");
            
            // 판매 후 인벤토리 UI 갱신
            PopulateSellPanel(); 
        }
        else
        {
            Debug.LogError("알 수 없는 오류로 판매에 실패했습니다.");
        }
    }
}
