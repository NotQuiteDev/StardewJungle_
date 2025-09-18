using UnityEngine;

// 이 스크립트는 Collider 컴포넌트가 반드시 필요함을 명시합니다.
[RequireComponent(typeof(Collider))]
public class Fan_Push : MonoBehaviour // MonoBehaviour를 상속받아야 합니다.
{
    [Header("Fan Settings")]
    [Tooltip("팬이 밀어내는 힘의 크기입니다.")]
    public float fanForce = 15f;

    [Header("Cylinder Area (Gizmo & Collider)")]
    [Tooltip("바람이 작용하는 원통의 반지름입니다.")]
    public float radius = 2f;
    [Tooltip("바람이 작용하는 원통의 높이입니다.")]
    public float height = 5f;

    // 물리 효과는 고정된 시간 간격으로 처리하는 것이 좋습니다.
    // Update 대신 OnTriggerStay를 사용하므로 여기서는 FixedUpdate가 필수는 아닙니다.
    // 하지만 힘을 가하는 로직은 OnTriggerStay에 구현합니다.


    /// <summary>
    /// 트리거 영역 안에 다른 Collider가 머무는 동안 매 프레임 호출됩니다.
    /// </summary>
    /// <param name="other">영역 안에 들어온 다른 객체의 Collider</param>
    private void OnTriggerStay(Collider other)
    {
        // 들어온 객체에서 Rigidbody 컴포넌트를 찾습니다.
        Rigidbody rb = other.GetComponent<Rigidbody>();

        // 만약 객체가 Rigidbody를 가지고 있다면 (물리 효과를 받는 객체라면)
        if (rb != null)
        {
            // 이 오브젝트의 위쪽 방향(Y축)으로 힘을 가합니다.
            // ForceMode.Force는 질량을 고려하여 힘을 가합니다. (기본값)
            // ForceMode.Acceleration은 질량을 무시하고 가속도를 적용합니다.
            rb.AddForce(transform.up * fanForce, ForceMode.Force);
        }
    }


    /// <summary>
    /// 씬(Scene) 에디터에서만 보이는 기즈모를 그리는 함수입니다.
    /// 개발자가 영역을 시각적으로 확인하기 위해 사용합니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        // 기즈모의 색상을 반투명한 파란색으로 설정합니다.
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.5f);
        
        // 기즈모의 좌표계를 이 오브젝트의 로컬 좌표계로 설정합니다.
        // 이렇게 하면 오브젝트가 회전하거나 이동해도 기즈모가 함께 따라 움직입니다.
        Gizmos.matrix = transform.localToWorldMatrix;

        // 원통의 위쪽과 아래쪽 중심 위치를 계산합니다.
        Vector3 topCenter = new Vector3(0, height / 2, 0);
        Vector3 bottomCenter = new Vector3(0, -height / 2, 0);

        // --- 원통 그리기 ---
        // 위쪽 원과 아래쪽 원을 그립니다. (단순화를 위해 점선으로 표현)
        // 실제 원통 기즈모를 그리려면 코드가 복잡해져서, 와이어 큐브로 대체하여 영역을 표시합니다.
        // 이는 의도한 원통 영역을 충분히 시각적으로 나타내 줍니다.
        Vector3 size = new Vector3(radius * 2, height, radius * 2);
        Gizmos.DrawWireCube(Vector3.zero, size);
        
        // 힘이 작용하는 방향을 화살표로 표시합니다.
        Gizmos.color = Color.yellow;
        Vector3 arrowEnd = topCenter + new Vector3(0, 1f, 0); // 원통 위로 뻗어나가는 화살표
        Gizmos.DrawLine(topCenter, arrowEnd);
        Gizmos.DrawLine(arrowEnd, arrowEnd - new Vector3(0.2f, -0.2f, 0));
        Gizmos.DrawLine(arrowEnd, arrowEnd - new Vector3(-0.2f, -0.2f, 0));
    }
}