using UnityEngine;

[CreateAssetMenu(fileName = "New Hoe", menuName = "Inventory/Hoe")]
public class TillingHoeData : ItemData
{
    [Header("Hoe Settings")]
    [Tooltip("경작지 프리팹(FarmPlot)")]
    public GameObject farmPlotPrefab;

    [Tooltip("한 번 휘두를 때 증가량 (0~1) — 예: 0.25면 4번 휘두르면 100%")]
    public float swingAdd01 = 0.25f;

    [Tooltip("한 번 휘두르고 다시 휘두를 수 있는 최소 간격(초)")]
    public float swingCooldown = 0.35f;

    [Tooltip("레이 사거리(카메라 기준)")]
    public float raycastDistance = 5f;

    [Tooltip("새 경작지 생성 최소 간격(주변 이 거리 이내 기존 경작지 있으면 생성 금지)")]
    public float minSeparation = 1.0f;

    [Tooltip("격자 스냅 크기(0이면 스냅 안함)")]
    public float gridSize = 1.0f;

    [Tooltip("지면 높이 보정")]
    public float spawnHeightOffset = 0.05f;

    [Header("Layer Masks")]
    [Tooltip("경작 가능한 지면 레이어")]
    public LayerMask groundMask;
    [Tooltip("기존 경작지 레이어(FarmPlot)")]
    public LayerMask farmPlotMask;

    // 단발 사용 없음
    public override void Use(Transform equipPoint, Transform cameraTransform) { }

    // 클릭 시작 시 한 번만 실행
    public override void BeginUse(Transform equipPoint, Transform cameraTransform, MonoBehaviour runner)
    {
        if (runner == null) return;
        var runtime = runner.GetComponent<TillingHoeRuntime>();
        if (runtime == null) runtime = runner.gameObject.AddComponent<TillingHoeRuntime>();

        // ★ 누르는 순간 즉시 1회 스윙 + 홀드 시작
        runtime.BeginTilling(this, equipPoint, cameraTransform);
    }

    public override void EndUse()
    {
        // 호출부(PlayerMovement)에서 Stop 호출
        // 여긴 그대로 두되, 실제 정지는 런타임에서 처리
    }
}
