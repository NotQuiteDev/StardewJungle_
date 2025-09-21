using UnityEngine;
using System.Collections.Generic;

public class BlacksmithNPC : MonoBehaviour, IInteractable
{
    [Header("NPC 정보")]
    [SerializeField] private string npcName = "Blacksmith";
    [Tooltip("UI에 표시될 NPC의 얼굴 그림(초상화)")]
    public Sprite npcPortrait;

    [Header("업그레이드 레시피")]
    [Tooltip("이 대장장이가 제공하는 업그레이드 목록")]
    [SerializeField] private List<UpgradeRecipeData> availableRecipes;

    public string GetInteractionText() => $"Talk to {npcName}";

    public void Interact()
    {
        // UI 매니저를 호출하여 업그레이드 창을 연다.
        UpgradeUIManager.Instance.OpenUpgradeUI(this);
    }

    public List<UpgradeRecipeData> GetAvailableRecipes()
    {
        return availableRecipes;
    }
}