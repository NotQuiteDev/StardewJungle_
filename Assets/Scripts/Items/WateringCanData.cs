using UnityEngine;

[CreateAssetMenu(fileName = "New Watering Can", menuName = "Inventory/Watering Can")]
public class WateringCanData : ItemData
{
    [Header("물뿌리개 전용 설정")]
    [Tooltip("분사 파티클 프리팹(Particle System 포함된 GO)")]
    public GameObject waterParticlesPrefab;

    [Tooltip("초당 물 주입량 (CropManager 단위에 맞추기)")]
    public float waterPerSecond = 5f;

    [Tooltip("레이 사거리(카메라 기준)")]
    public float raycastDistance = 5f;

    [Header("분사 영역(원통)")]
    [Tooltip("원통(캡슐) 반지름")]
    public float sprayRadius = 0.6f;

    [Tooltip("원통(캡슐) 길이 (카메라/노즐 전방)")]
    public float sprayLength = 4f;

    [Tooltip("물을 받을 수 있는 레이어 (비우면 전부)")]
    public LayerMask waterableLayer;

    [Tooltip("여러 작물을 동시에 적실 때, 초당 총량을 동일 분배할지 여부")]
    public bool distributeWaterEvenly = true;

    // 단발 사용은 비워도 됨(호환용)
    public override void Use(Transform equipPoint, Transform cameraTransform) { }

    // 홀드 시작: 실행용 컴포넌트에 위임
    public override void BeginUse(Transform equipPoint, Transform cameraTransform, MonoBehaviour runner)
    {
        if (runner == null) return;
        var runtime = runner.GetComponent<WateringCanRuntime>();
        if (runtime == null) runtime = runner.gameObject.AddComponent<WateringCanRuntime>();
        runtime.StartWatering(this, equipPoint, cameraTransform);
    }

    // 홀드 종료: 실행 컴포넌트에 정지 요청
    public override void EndUse() { /* 호출부에서 StopWatering() 호출됨 */ }
}
