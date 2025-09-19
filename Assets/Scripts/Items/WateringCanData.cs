using UnityEngine;

[CreateAssetMenu(fileName = "New Watering Can", menuName = "Inventory/Watering Can")]
public class WateringCanData : ItemData
{
    [Header("물뿌리개 전용 설정")]
    [Tooltip("분사 파티클 프리팹(Particle System 포함된 GO)")]
    public GameObject waterParticlesPrefab;

    [Tooltip("초당 물 주입량 (CropManager가 기대하는 단위에 맞추기)")]
    public float waterPerSecond = 5f;

    [Tooltip("레이 사거리(카메라 기준)")]
    public float raycastDistance = 5f;

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

    // 홀드 종료는 호출부(플레이어)에서 runtime.StopWatering()로 처리
    public override void EndUse() { }
}
