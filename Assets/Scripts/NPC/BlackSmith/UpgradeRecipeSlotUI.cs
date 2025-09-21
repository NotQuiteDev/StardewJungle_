using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeRecipeSlotUI : MonoBehaviour
{
    [SerializeField] private Image resultItemIcon;
    [SerializeField] private TextMeshProUGUI resultItemNameText;
    [SerializeField] private Button selectButton;

    private UpgradeRecipeData currentRecipe;

    public void Setup(UpgradeRecipeData recipe)
    {
        currentRecipe = recipe;
        resultItemIcon.sprite = recipe.resultItem.itemIcon;
        resultItemNameText.text = recipe.resultItem.itemName;
        selectButton.onClick.AddListener(OnSelectButtonClick);
    }

    private void OnSelectButtonClick()
    {
        UpgradeUIManager.Instance.OnRecipeSelected(currentRecipe);
    }
}