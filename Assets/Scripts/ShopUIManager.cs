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
    [SerializeField] private MoneyDisplayUI moneyDisplayUI; // ## 추가: 돈 UI 직접 연결 ##

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
        npcPortraitImage.sprite = currentMerchant.npcPortrait;
        
        RefreshUI(); // UI 전체 새로고침
    }

    public void CloseShop()
    {
        GameManager.Instance.EnterGameplayMode();
        currentMerchant = null;
        shopWindowObject.SetActive(false);
    }
    
    // UI 전체를 새로고침하는 함수
    private void RefreshUI()
    {
        PopulateBuyPanel(currentMerchant.shopItemList);
        PopulateSellPanel();
        moneyDisplayUI?.ForceUpdate(); // 돈 UI 강제 업데이트
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

    public void TryPurchaseItem(ShopItem shopItem)
    {
        if (MoneyManager.Instance.SpendMoney(shopItem.price))
        {
            if (InventoryManager.Instance.AddItem(shopItem.item, 1))
            {
                Debug.Log($"Purchased {shopItem.item.itemName} successfully!");
                RefreshUI(); // 거래 성공 후 UI 전체 새로고침
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

    public void TrySellItem(ItemData itemData, int quantity)
    {
        if (InventoryManager.Instance.RemoveItem(itemData, quantity))
        {
            int sellPrice = itemData.sellPrice * quantity;
            MoneyManager.Instance.AddMoney(sellPrice);
            Debug.Log($"Sold {itemData.itemName} x{quantity} for ${sellPrice}.");
            RefreshUI(); // 거래 성공 후 UI 전체 새로고침
        }
    }
}