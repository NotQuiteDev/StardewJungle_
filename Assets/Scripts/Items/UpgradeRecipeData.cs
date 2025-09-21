using UnityEngine;

[CreateAssetMenu(fileName = "New Upgrade Recipe", menuName = "NPC/Upgrade Recipe")]
public class UpgradeRecipeData : ScriptableObject
{
    [Header("업그레이드 정보")]
    [Tooltip("업그레이드할 기본 도구 (예: 일반 곡괭이)")]
    public ItemData baseItem;

    [Tooltip("업그레이드에 필요한 재료 (예: 구리 주괴)")]
    public ItemData requiredMaterial;

    [Tooltip("필요한 재료의 개수")]
    public int requiredMaterialCount;

    [Tooltip("업그레이드 비용")]
    public int requiredMoney;

    [Header("결과물")]
    [Tooltip("업그레이드 후 받게 될 아이템 (예: 구리 곡괭이)")]
    public ItemData resultItem;
}