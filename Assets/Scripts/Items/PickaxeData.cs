using UnityEngine;

[CreateAssetMenu(fileName = "New Pickaxe", menuName = "Inventory/Pickaxe")]
public class PickaxeData : ItemData
{
    [Header("Pickaxe Settings")]
    [Tooltip("한 번 휘두를 때 돌에 가하는 데미지")]
    public float damagePerSwing = 25f;

    [Tooltip("연속 휘두르기 최소 간격(초)")]
    public float swingCooldown = 0.5f;

    [Tooltip("돌을 감지할 레이의 사거리")]
    public float raycastDistance = 5f;

    [Header("Layer Mask")]
    [Tooltip("채광 가능한 돌이 있는 레이어")]
    public LayerMask mineableMask;

    // 단발 사용은 없음
    public override void Use(Transform equipPoint, Transform cameraTransform) { }

    // 마우스 클릭을 시작했을 때 호출됨
    public override void BeginUse(Transform equipPoint, Transform cameraTransform, MonoBehaviour runner)
    {
        if (runner == null) return;
        
        // 플레이어에게 PickaxeRuntime이 없으면 추가하고, 있으면 가져옴
        var runtime = runner.GetComponent<PickaxeRuntime>();
        if (runtime == null) runtime = runner.gameObject.AddComponent<PickaxeRuntime>();

        // 런타임에 채광 시작 명령
        runtime.BeginMining(this, equipPoint, cameraTransform);
    }

    // 마우스 클릭을 멈췄을 때 호출됨 (실제 로직은 PlayerMovement에서 처리)
    public override void EndUse() { }
}