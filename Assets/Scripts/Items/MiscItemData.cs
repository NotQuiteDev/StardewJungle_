using UnityEngine;

/// <summary>
/// 사용할 수는 없지만 인벤토리에 보관하거나 판매할 수 있는 
/// '기타' 아이템을 위한 데이터입니다. (예: 돌멩이, 원석, 재료 아이템 등)
/// </summary>
[CreateAssetMenu(fileName = "New Misc Item", menuName = "Inventory/Misc Item")]
public class MiscItemData : ItemData
{
    [Header("기타 아이템 정보")]
    [TextArea(3, 5)] // 인스펙터에서 여러 줄 텍스트를 편하게 입력하도록 합니다.
    public string itemDescription = "설명을 입력하세요.";

    /// <summary>
    /// ## '사용 불가능'의 핵심 ##
    /// 이 함수를 의도적으로 비워두어, 플레이어가 사용 버튼을 눌러도
    /// 아무런 일이 일어나지 않도록 만듭니다.
    /// </summary>
    public override void Use(Transform equipPoint, Transform cameraTransform)
    {
        // 의도적으로 비워둠.
        // 필요하다면 Debug.Log($"{this.itemName}은(는) 사용할 수 없는 아이템입니다."); 같은
        // 디버그 메시지를 넣어 테스트할 수 있습니다.
    }

    // 기타 아이템은 홀드/지속 사용 기능이 필요 없으므로 비워둡니다.
    public override void BeginUse(Transform equipPoint, Transform cameraTransform, MonoBehaviour runner) { }
    public override void EndUse() { }
}