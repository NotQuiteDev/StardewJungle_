using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class UpgradeUIManager : MonoBehaviour
{
    public static UpgradeUIManager Instance { get; private set; }

    [Header("UI 창")]
    [SerializeField] private GameObject upgradeWindow;

    [Header("패널 및 프리팹")]
    [SerializeField] private Transform recipeContentPanel;
    [SerializeField] private GameObject recipeSlotPrefab;
    [SerializeField] private GameObject requirementsPanel;

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

    // ## 추가: 게임 시작 시 UI를 숨기기 위한 Start 함수 ##
    private void Start()
    {
        upgradeWindow.SetActive(false);
    }

    public void OpenUpgradeUI(BlacksmithNPC blacksmith)
    {
        // ## 추가: 게임을 'UI 모드'로 전환 (마우스 커서 보이기, 게임 일시정지) ##
        GameManager.Instance.EnterUIMode();

        currentRecipes = blacksmith.GetAvailableRecipes();
        npcPortrait.sprite = blacksmith.npcPortrait;
        upgradeWindow.SetActive(true);

        PopulateRecipeList();
        ClearRequirementsPanel();
    }

    public void CloseUpgradeUI()
    {
        // ## 추가: 게임을 '게임플레이 모드'로 전환 (마우스 커서 숨기기, 게임 재개) ##
        GameManager.Instance.EnterGameplayMode();

        upgradeWindow.SetActive(false);
    }

    private void PopulateRecipeList()
    {
        foreach (Transform child in recipeContentPanel) { Destroy(child.gameObject); }
        foreach (var recipe in currentRecipes)
        {
            GameObject slotGO = Instantiate(recipeSlotPrefab, recipeContentPanel);
            slotGO.GetComponent<UpgradeRecipeSlotUI>().Setup(recipe);
        }
    }
    
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
        
        UpdateRequirementsPanel();
    }

    private void ClearRequirementsPanel()
    {
        requirementsPanel.SetActive(false);
        selectedRecipe = null;
        upgradeButton.interactable = false;
    }
}