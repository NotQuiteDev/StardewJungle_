using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class UpgradeUIManager : MonoBehaviour
{
    public static UpgradeUIManager Instance { get; private set; }

    [Header("UI 창")]
    [SerializeField] private GameObject upgradeWindow;

    // ## 수정: 좌측 패널(플레이어 인벤토리) 관련 변수 모두 삭제 ##
    // [SerializeField] private Transform playerInventoryContent;
    // [SerializeField] private GameObject playerItemSlotPrefab;

    [Header("패널 및 프리팹")]
    [SerializeField] private Transform recipeContentPanel;     // 중앙 패널
    [SerializeField] private GameObject recipeSlotPrefab;
    [SerializeField] private GameObject requirementsPanel;      // 우측 패널

    [Header("UI 요소 연결")]
    [SerializeField] private Image npcPortrait;
    [SerializeField] private Image resultItemIcon;
    [SerializeField] private TextMeshProUGUI resultItemName;
    [SerializeField] private Image baseItemIcon;
    [SerializeField] private TextMeshProUGUI baseItemText;
    [SerializeField] private Image materialIcon;
    [SerializeField] private TextMeshProUGUI materialText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;

    private List<UpgradeRecipeData> currentRecipes;
    private UpgradeRecipeData selectedRecipe;

    private void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
        upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        closeButton.onClick.AddListener(CloseUpgradeUI);
    }

    public void OpenUpgradeUI(BlacksmithNPC blacksmith)
    {
        currentRecipes = blacksmith.GetAvailableRecipes();
        npcPortrait.sprite = blacksmith.npcPortrait;
        upgradeWindow.SetActive(true);
        // GameManager.Instance.EnterUIMode();

        // ## 수정: 플레이어 인벤토리 채우는 함수 호출 삭제 ##
        PopulateRecipeList();
        ClearRequirementsPanel();
    }

    public void CloseUpgradeUI()
    {
        upgradeWindow.SetActive(false);
        // GameManager.Instance.EnterGameplayMode();
    }
    
    // ## 삭제: 이 함수는 더 이상 필요 없습니다. ##
    // private void PopulatePlayerInventory() { ... }

    private void PopulateRecipeList()
    {
        foreach (Transform child in recipeContentPanel) { Destroy(child.gameObject); }
        foreach (var recipe in currentRecipes)
        {
            GameObject slotGO = Instantiate(recipeSlotPrefab, recipeContentPanel);
            slotGO.GetComponent<UpgradeRecipeSlotUI>().Setup(recipe);
        }
    }

    // ## 삭제: 이 함수는 더 이상 필요 없습니다. ##
    // public void OnPlayerItemClicked(ItemData selectedItem) { ... }

    public void OnRecipeSelected(UpgradeRecipeData recipe)
    {
        selectedRecipe = recipe;
        requirementsPanel.SetActive(true);
        UpdateRequirementsPanel();
    }

    private void UpdateRequirementsPanel()
    {
        if (selectedRecipe == null)
        {
            ClearRequirementsPanel();
            return;
        }

        resultItemIcon.sprite = selectedRecipe.resultItem.itemIcon;
        resultItemName.text = selectedRecipe.resultItem.itemName;

        int baseItemCount = InventoryManager.Instance.GetItemCount(selectedRecipe.baseItem);
        int materialCount = InventoryManager.Instance.GetItemCount(selectedRecipe.requiredMaterial);
        int moneyAmount = MoneyManager.Instance.CurrentMoney;

        baseItemIcon.sprite = selectedRecipe.baseItem.itemIcon;
        baseItemText.text = $"{baseItemCount} / 1";

        materialIcon.sprite = selectedRecipe.requiredMaterial.itemIcon;
        materialText.text = $"{materialCount} / {selectedRecipe.requiredMaterialCount}";
        
        moneyText.text = $"$ {moneyAmount:N0} / {selectedRecipe.requiredMoney:N0}";

        bool hasBaseItem = baseItemCount >= 1;
        bool hasMaterials = materialCount >= selectedRecipe.requiredMaterialCount;
        bool hasMoney = moneyAmount >= selectedRecipe.requiredMoney;

        baseItemText.color = hasBaseItem ? Color.white : Color.red;
        materialText.color = hasMaterials ? Color.white : Color.red;
        moneyText.color = hasMoney ? Color.white : Color.red;

        upgradeButton.interactable = hasBaseItem && hasMaterials && hasMoney;
    }

    public void OnUpgradeButtonClicked()
    {
        if (selectedRecipe == null || !upgradeButton.interactable) return;

        InventoryManager.Instance.RemoveItem(selectedRecipe.baseItem, 1);
        InventoryManager.Instance.RemoveItem(selectedRecipe.requiredMaterial, selectedRecipe.requiredMaterialCount);
        MoneyManager.Instance.SpendMoney(selectedRecipe.requiredMoney);
        InventoryManager.Instance.AddItem(selectedRecipe.resultItem, 1);
        
        Debug.Log($"{selectedRecipe.resultItem.itemName}(으)로 업그레이드 성공!");

        // 거래 후 UI 새로고침 (플레이어 인벤토리 목록은 없으니 이 함수만 호출)
        UpdateRequirementsPanel();
    }

    private void ClearRequirementsPanel()
    {
        requirementsPanel.SetActive(false);
        selectedRecipe = null;
        upgradeButton.interactable = false;
    }
}