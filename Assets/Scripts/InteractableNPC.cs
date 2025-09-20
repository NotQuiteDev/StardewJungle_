using UnityEngine;
using System.Collections.Generic; // List를 사용하기 위해 필요

// ------------------------------------------------------------------
// ## 1. 상점 판매 아이템의 데이터 구조 정의 ##
// InteractableNPC 클래스 바깥이나 안쪽에 정의할 수 있어.
// ------------------------------------------------------------------
[System.Serializable]
public class ShopItem
{
    [Tooltip("판매할 아이템 (ItemData 에셋)")]
    public ItemData item;
    [Tooltip("플레이어가 구매할 때의 가격 (구매가)")]
    public int price;
}

// ------------------------------------------------------------------
// ## 2. 기존 NPC 스크립트 확장 ##
// ------------------------------------------------------------------
public class InteractableNPC : MonoBehaviour, IInteractable
{
    [Header("NPC 공통 설정")]
    [Tooltip("플레이어에게 표시될 상호작용 텍스트 (예: Open Shop, Talk)")]
    [SerializeField] private string interactionPrompt = "Interact";

    // ## 추가: NPC 얼굴 그림 ##
    [Header("상점 NPC 전용 설정")]
    [Tooltip("상점 UI에 표시될 NPC의 얼굴 그림(초상화)")]
    public Sprite npcPortrait;

    // ## 추가: 상점 판매 목록 ##
    [Tooltip("이 NPC가 판매할 아이템 목록")]
    public List<ShopItem> shopItemList;


    public string GetInteractionText()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        // 기존의 Debug.Log 대신, ShopUIManager의 OpenShop 함수를 호출한다.
        // 'this'는 "나 자신", 즉 이 스크립트가 붙어있는 NPC 자체를 의미.
        ShopUIManager.Instance.OpenShop(this);
    }
}