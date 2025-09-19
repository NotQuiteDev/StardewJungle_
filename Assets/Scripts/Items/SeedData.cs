using UnityEngine;

[CreateAssetMenu(fileName = "New Seed", menuName = "Inventory/Seed")]
public class SeedData : ItemData
{
    [Header("Planting")]
    [Tooltip("심을 식물 프리팹 (CropManager 포함)")]
    public GameObject plantPrefab;

    [Tooltip("카메라 기준 레이 사거리")]
    public float raycastDistance = 5f;

    [Tooltip("FarmPlot(Interactable) 레이어 마스크")]
    public LayerMask farmPlotMask;

    [Tooltip("심을 때 Y 오프셋 (앵커 없을 때만 사용)")]
    public float plantYOffset = 0.05f;

    [Header("UI")]
    [Tooltip("아이콘에 적용할 색상 틴트 (흰색 원본 위에 곱)")]
    public Color iconTint = Color.white;

    // 클릭 1회로 심기
    public override void Use(Transform equipPoint, Transform cameraTransform)
    {
        if (plantPrefab == null || cameraTransform == null) return;

        // 1) 카메라 전방 → FarmPlot만 레이캐스트
        if (!Physics.Raycast(new Ray(cameraTransform.position, cameraTransform.forward),
                             out RaycastHit hit, raycastDistance, farmPlotMask))
            return;

        var plot = hit.collider.GetComponentInParent<FarmPlot>();
        if (plot == null) return;

        // 2) 완전 경작 & 미점유만 허용
        if (!plot.IsFullyTilled) return;
        if (plot.HasAnyCrop())   return; // 이미 식물 있음

        // 3) 스폰 위치: Anchor가 있으면 거기, 없으면 중앙 + y 오프셋
        Vector3 pos = plot.GetPlantSpawnPoint(plantYOffset);
        Quaternion rot = plot.transform.rotation;

        // 4) 심기(플롯의 자식으로)
        var go = Object.Instantiate(plantPrefab, pos, rot, plot.transform);
        if (go.GetComponent<CropManager>() == null)
        {
            Debug.LogWarning("Planted prefab has no CropManager.");
        }

        // 5) 씨앗 1개 소비 (현재 포커스 슬롯에서)
        var inv = equipPoint != null ? equipPoint.GetComponentInParent<InventoryManager>() : null;
        if (inv != null) inv.ConsumeFocusedItem(1);
    }

    // 씨앗은 홀드/지속 사용 아님
    public override void BeginUse(Transform equipPoint, Transform cameraTransform, MonoBehaviour runner) { }
    public override void EndUse() { }
}
