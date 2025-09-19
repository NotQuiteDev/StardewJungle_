using UnityEngine;

// 이 줄은 ScriptableObject를 유니티 에디터의 Create 메뉴에 추가해주는 역할입니다.
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("아이템 정보")]
    public string itemName; // 아이템 이름
    public Sprite itemIcon; // 인벤토리 슬롯에 표시될 2D 아이콘
    public GameObject itemPrefab; // 플레이어 손에 들게 될 3D 모델 (프리팹)
}