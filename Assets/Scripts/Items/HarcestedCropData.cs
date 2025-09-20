using UnityEngine;

[CreateAssetMenu(fileName = "New Harvested Crop", menuName = "Inventory/Harvested Crop")]
public class HarvestedCropData : ItemData
{
    [Header("수확물 정보")]
    public int sellPrice;

    // 수확된 작물은 인벤토리에서 직접 '사용'하는 기능이 없으므로,
    // 부모의 추상 함수들을 비워둡니다.
    public override void Use(Transform equipPoint, Transform cameraTransform)
    {
        Debug.Log($"{itemName}은(는) 사용할 수 없습니다. 상점에 판매하세요.");
    }

    public override void BeginUse(Transform equipPoint, Transform cameraTransform, MonoBehaviour runner) { }
    public override void EndUse() { }
}