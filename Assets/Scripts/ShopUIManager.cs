using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    [Header("UI 연결")]
    [SerializeField] private GameObject shopWindowObject;
    [SerializeField] private Image npcPortraitImage;
    [SerializeField] private Transform buyPanelContent;
    [SerializeField] private Transform sellPanelContent;
    [SerializeField] private Button closeButton;
    [SerializeField] private MoneyDisplayUI moneyDisplayUI;

    [Header("프리팹 연결")]
    [SerializeField] private GameObject buySlotPrefab;
    [SerializeField] private GameObject sellSlotPrefab;

    private InteractableNPC currentMerchant;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseShop);
        }
        shopWindowObject.SetActive(false);
    }

    public void OpenShop(InteractableNPC merchant)
    {
        GameManager.Instance.EnterUIMode();
        currentMerchant = merchant;
        shopWindowObject.SetActive(true);
        
        // ## 수정: merchant에서 직접 초상화를 가져오는 대신, DialogueInfo를 통해 가져옵니다. ##
        if (merchant.DialogueInfo != null)
        {
            npcPortraitImage.sprite = merchant.DialogueInfo.npcPortrait;
        }
        
        RefreshUI();
    }

    public void CloseShop()
    {
        GameManager.Instance.EnterGameplayMode();
        currentMerchant = null;
        shopWindowObject.SetActive(false);
    }
    
    public void RefreshUI()
    {
        PopulateBuyPanel(currentMerchant.shopItemList);
        PopulateSellPanel();
        moneyDisplayUI?.ForceUpdate();
    }

    private void PopulateBuyPanel(List<ShopItem> items)
    {
        foreach (Transform child in buyPanelContent) { Destroy(child.gameObject); }
        foreach (var item in items)
        {
            GameObject slotInstance = Instantiate(buySlotPrefab, buyPanelContent);
            slotInstance.GetComponent<ShopSlotUI>().Setup(item);
        }
    }

    private void PopulateSellPanel()
    {
        foreach (Transform child in sellPanelContent) { Destroy(child.gameObject); }
        var inventoryData = InventoryManager.Instance.inventorySlotsData;
        var inventoryCounts = InventoryManager.Instance.slotCounts;
        for (int i = 0; i < inventoryData.Length; i++)
        {
            if (inventoryData[i] != null)
            {
                GameObject slotInstance = Instantiate(sellSlotPrefab, sellPanelContent);
                slotInstance.GetComponent<PlayerInventorySlotUI>().Setup(inventoryData[i], inventoryCounts[i]);
            }
        }
    }

    // ## 기존 함수 수정: 성공 시 true, 실패 시 false를 반환하도록 변경 ##
    public bool TryPurchaseItem(ShopItem shopItem)
    {
        if (!MoneyManager.Instance.SpendMoney(shopItem.price))
        {
            Debug.Log("Not enough money to purchase.");
            return false; // 돈 부족으로 실패
        }

        if (InventoryManager.Instance.AddItem(shopItem.item, 1))
        {
            Debug.Log($"Purchased {shopItem.item.itemName} successfully!");
            // RefreshUI(); // 루프 안에서는 새로고침을 하지 않음
            return true; // 구매 성공
        }
        else
        {
            Debug.Log("Purchase failed. Inventory is full. Refunding money.");
            MoneyManager.Instance.AddMoney(shopItem.price); // 돈 환불
            return false; // 인벤토리 부족으로 실패
        }
    }

    // ## 추가: '모두 구매'를 처리할 새 함수 ##
    public void TryPurchaseMaxAffordable(ShopItem shopItem)
    {
        int purchasedCount = 0;
        // TryPurchaseItem이 성공하는 동안(true를 반환하는 동안) 계속 반복
        while (TryPurchaseItem(shopItem))
        {
            purchasedCount++;
        }

        // 루프가 끝난 후(한 번이라도 구매에 성공했다면) UI를 한 번만 새로고침
        if (purchasedCount > 0)
        {
            Debug.Log($"Transaction finished. Purchased a total of {purchasedCount} items.");
            RefreshUI();
        }
        else
        {
            // 1개도 못 샀을 경우 (보통 버튼 비활성화로 막아주지만, 만약을 대비)
            Debug.Log("Could not purchase any items.");
        }
    }

    public void TrySellItem(ItemData itemData, int quantity)
    {
        if (InventoryManager.Instance.RemoveItem(itemData, quantity))
        {
            int sellPrice = itemData.sellPrice * quantity;
            MoneyManager.Instance.AddMoney(sellPrice);
            Debug.Log($"Sold {itemData.itemName} x{quantity} for ${sellPrice}.");
            RefreshUI();
        }
    }

    // ## 추가: ShopSlotUI에서 버튼 활성화 여부를 묻기 위한 함수 ##
    public bool CanAfford(int price)
    {
        return MoneyManager.Instance.CurrentMoney >= price;
    }
}