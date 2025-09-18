using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LaserBeam : MonoBehaviour
{
    [Header("레이저 설정")]
    [Tooltip("레이저가 부딪힐 대상을 인식할 레이어입니다.")]
    [SerializeField] private LayerMask collisionLayer;
    [Tooltip("레이저의 최대 사거리입니다.")]
    [SerializeField] private float maxDistance = 100f;

    // ★★★ 추가: 레이저 굵기(스케일)를 제어할 변수들 ★★★
    [Header("레이저 스케일 설정")]
    [Tooltip("레이저가 생성될 때의 초기 XY 스케일(굵기)입니다.")]
    [SerializeField] private float startScale = 0.1f;
    [Tooltip("레이저가 최대로 커질 XY 스케일(굵기)입니다.")]
    [SerializeField] private float endScale = 1.0f;
    [Tooltip("레이저가 최대로 커지는 데 걸리는 시간입니다.")]
    [SerializeField] private float scaleDuration = 1.0f;

    private Transform firePoint;
    private float scaleTimer = 0f; // ★★★ 스케일 변화를 위한 타이머 ★★★

    // 초기화 함수
    public void Initialize(Transform ownerFirePoint)
    {
        firePoint = ownerFirePoint;
        // ★★★ 시작할 때 초기 스케일을 적용합니다 ★★★
        transform.localScale = new Vector3(startScale, startScale, transform.localScale.z);
    }

    void Update()
    {
        if (firePoint == null) return;

        // 1. Raycast를 쏴서 레이저의 '끝점(endPoint)'을 찾는다.
        RaycastHit hit;
        Vector3 endPoint;
        // firePoint.forward 대신 -firePoint.forward (뒷방향)으로 Raycast를 앏니다.
        if (Physics.Raycast(firePoint.position, -firePoint.forward, out hit, maxDistance, collisionLayer))
        {
            endPoint = hit.point;
        }
        else
        {
            // Raycast가 아무것도 맞추지 못하면, 뒷방향으로 maxDistance만큼 뻗어나갑니다.
            endPoint = firePoint.position + (-firePoint.forward * maxDistance);
        }

        // 2. 레이저의 '시작점'과 '끝점'을 바탕으로 길이와 중심점을 계산한다.
        float beamLength = Vector3.Distance(firePoint.position, endPoint);
        Vector3 beamCenter = (firePoint.position + endPoint) / 2f;

        // 3. 계산된 값들을 Transform에 적용한다.
        transform.position = beamCenter;
        transform.LookAt(endPoint);
        
        // ★★★ 핵심 수정: X, Y, Z 스케일을 모두 함께 제어합니다 ★★★

        // 타이머를 증가시키되, 최대 시간을 넘지 않도록 합니다.
        scaleTimer += Time.deltaTime;
        
        // Lerp를 사용하여 시작 스케일에서 끝 스케일까지 부드럽게 보간합니다.
        float currentScale = Mathf.Lerp(startScale, endScale, scaleTimer / scaleDuration);

        // 최종적으로 계산된 스케일을 적용합니다.
        transform.localScale = new Vector3(currentScale, currentScale, beamLength);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerCheckpoint>()?.Respawn();
        }
    }
}