using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindEnemy : MonoBehaviour
{
    public Vector3 endPos;
    [SerializeField] private float detectionDistance = 10f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float rayInterval = 1f;
    [SerializeField] private float delayInterval = 3f;
    [SerializeField] private float forwardAngle = 15f;
    [SerializeField] private bool showArea = false;

    public float duration = 0.25f;
    public GameObject overUIModel;
    private Vector3 startPos;
    private bool isTarget = false;

    private List<Transform> detectedPlayers = new List<Transform>();
    private Coroutine detectionCoroutine;
    private GameObject Spot;

    void Start()
    {
        startPos = gameObject.transform.position;
        endPos = startPos - new Vector3(0, -0.576f, 0);
        detectionCoroutine = StartCoroutine(DetectionLoop());
        Spot = transform.GetChild(transform.childCount - 1).gameObject;
    }

    IEnumerator DetectionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(delayInterval);
            StartCoroutine(ChaseCatAnim(startPos, endPos));

            // 감지 시작
            showArea = true;
            Spot.SetActive(true);

            // rayInterval 시간 동안 계속 감지
            yield return StartCoroutine(ContinuousDetection());

            StartCoroutine(ChaseCatAnim(endPos, startPos));
            showArea = false;
            Spot.SetActive(false);
        }
    }

    // 일정 시간 동안 계속 감지하는 코루틴
    IEnumerator ContinuousDetection()
    {
        float elapsedTime = 0f;
        float checkInterval = 0.1f; // 0.1초마다 체크

        while (elapsedTime < rayInterval)
        {
            DetectAllObjectsInTriangle();
            yield return new WaitForSeconds(checkInterval);
            elapsedTime += checkInterval;
        }
    }

    IEnumerator ChaseCatAnim(Vector3 startPos, Vector3 endPos)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
    }

    void DetectAllObjectsInTriangle()
    {
        detectedPlayers.Clear();

        Collider[] allColliders = Physics.OverlapSphere(transform.position, detectionDistance);

        foreach (var collider in allColliders)
        {
            if (!collider.CompareTag(playerTag)) continue;

            // 수정: 콜라이더도 함께 전달
            if (IsInForwardRange(collider.transform.position, collider))
            {
                if (HasClearLineOfSight(collider.transform.position))
                {
                    if (isTarget)
                        return;
                    isTarget = true;
                    detectedPlayers.Add(collider.transform);
                    Debug.Log($"플레이어 발견: {collider.transform.name}");
                }
            }
        }
    }

    // 수정된 감지 로직 - 콜라이더 경계까지 고려
    bool IsInForwardRange(Vector3 targetPosition, Collider targetCollider)
    {
        // 콜라이더에서 내 위치까지 가장 가까운 점
        Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);

        Vector3 directionToClosest = (closestPoint - transform.position).normalized;
        float angleToClosest = Vector3.Angle(transform.forward, directionToClosest);
        float distanceToClosest = Vector3.Distance(transform.position, closestPoint);

        // 가장 가까운 점이 범위 안에 있으면 감지
        bool isClosestPointInRange = angleToClosest <= forwardAngle && distanceToClosest <= detectionDistance;

        // 추가 체크: 중심점도 확인 (안전장치)
        Vector3 directionToCenter = (targetPosition - transform.position).normalized;
        float angleToCenter = Vector3.Angle(transform.forward, directionToCenter);
        float distanceToCenter = Vector3.Distance(transform.position, targetPosition);
        bool isCenterInRange = angleToCenter <= forwardAngle && distanceToCenter <= detectionDistance;

        return isClosestPointInRange || isCenterInRange;
    }

    bool HasClearLineOfSight(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToTarget, out hit, distanceToTarget))
        {
            return hit.collider.CompareTag(playerTag);
        }

        return true;
    }

    // 수정된 기즈모 - 실제 3D 원뿔 범위 표시
    private void OnDrawGizmos()
    {
        if (!showArea) return;

        Gizmos.color = Color.yellow;

        // 3D 원뿔 시각화 (여러 방향으로 선 그리기)
        int rayCount = 12; // 원뿔을 그릴 선의 개수

        for (int i = 0; i < rayCount; i++)
        {
            // 원형으로 각도 분배
            float angle = (360f / rayCount) * i;

            // 원뿔 가장자리 점 계산
            Vector3 coneDirection = Quaternion.AngleAxis(angle, transform.forward) *
                                   (Quaternion.AngleAxis(forwardAngle, transform.up) * transform.forward);

            Vector3 conePoint = transform.position + coneDirection * detectionDistance;

            // 중심에서 원뿔 가장자리로 선 그리기
            Gizmos.DrawLine(transform.position, conePoint);
        }

        // 원뿔 끝부분 원 그리기
        Vector3 coneEndCenter = transform.position + transform.forward * detectionDistance;
        float coneEndRadius = detectionDistance * Mathf.Sin(forwardAngle * Mathf.Deg2Rad);

        // 원뿔 끝부분 와이어 원
        Gizmos.color = Color.cyan;
        for (int i = 0; i < 20; i++)
        {
            float angle1 = (360f / 20) * i * Mathf.Deg2Rad;
            float angle2 = (360f / 20) * (i + 1) * Mathf.Deg2Rad;

            Vector3 point1 = coneEndCenter + transform.right * Mathf.Cos(angle1) * coneEndRadius +
                            transform.up * Mathf.Sin(angle1) * coneEndRadius;
            Vector3 point2 = coneEndCenter + transform.right * Mathf.Cos(angle2) * coneEndRadius +
                            transform.up * Mathf.Sin(angle2) * coneEndRadius;

            Gizmos.DrawLine(point1, point2);
        }

        // 감지된 플레이어들 표시
        Gizmos.color = Color.red;
        foreach (var player in detectedPlayers)
        {
            if (player != null)
            {
                Gizmos.DrawLine(transform.position, player.position);
                Gizmos.DrawWireSphere(player.position, 0.5f);

                // 콜라이더의 가장 가까운 점도 표시
                Collider playerCollider = player.GetComponent<Collider>();
                if (playerCollider != null)
                {
                    Vector3 closestPoint = playerCollider.ClosestPoint(transform.position);
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(closestPoint, 0.2f);
                    Gizmos.DrawLine(transform.position, closestPoint);
                }

                // 각도 표시 (디버그용)
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);
            }
        }

        // 기즈모 색상 복원
        Gizmos.color = Color.red;
    }
}