using UnityEngine;

public class CropManager : MonoBehaviour
{
    [Header("성장 설정 (Growth Settings)")]
    [Tooltip("성장이 완료되는 데 걸리는 총 시간 (초)")]
    [SerializeField] private float growthDuration = 60f;
    [Tooltip("심겨졌을 때의 시작 크기")]
    [SerializeField] private Vector3 startScale = new Vector3(0.1f, 0.1f, 0.1f);
    [Tooltip("성장이 완료되었을 때의 최대 크기")]
    [SerializeField] private Vector3 maxScale = Vector3.one;

    [Header("수분 설정 (Water Settings)")]
    [Tooltip("작물이 죽는 최소 함수량")]
    [SerializeField] private float minWaterAmount = 0f;
    [Tooltip("작물이 죽는 최대 함수량")]
    [SerializeField] private float maxWaterAmount = 100f;
    [Tooltip("매초 잃는 함수량")]
    [SerializeField] private float waterLossPerSecond = 1f;

    [Header("상태 및 점수 (State & Scoring)")]
    [Tooltip("작물이 죽었을 때 대체될 프리팹")]
    [SerializeField] private GameObject deadCropPrefab;

    // --- 내부 변수 (Private Variables) ---
    public float CurrentWaterAmount { get; private set; }
    public float CurrentScore { get; private set; }
    
    private float optimalWaterAmount;
    private float waterRange;
    private float growthTimer;
    private bool isDead = false;

    private void Start()
    {
        // 초기화
        transform.localScale = startScale;
        
        // 최적 함수량 계산 및 설정
        optimalWaterAmount = (minWaterAmount + maxWaterAmount) / 2f;
        waterRange = (maxWaterAmount - minWaterAmount) / 2f; // 점수 계산을 위한 범위
        CurrentWaterAmount = optimalWaterAmount;

        // 1초마다 반복 실행할 함수 등록
        InvokeRepeating(nameof(UpdatePerSecond), 1f, 1f);
    }

    private void Update()
    {
        if (isDead) return;

        // 성장 처리 (부드러운 크기 변화)
        if (growthTimer < growthDuration)
        {
            growthTimer += Time.deltaTime;
            float growthPercent = Mathf.Clamp01(growthTimer / growthDuration);
            transform.localScale = Vector3.Lerp(startScale, maxScale, growthPercent);
        }
    }

    /// <summary>
    /// 1초마다 호출되는 업데이트 함수 (수분 감소, 점수 계산, 죽음 판정)
    /// </summary>
    private void UpdatePerSecond()
    {
        if (isDead) return;

        // 1. 수분 감소
        CurrentWaterAmount -= waterLossPerSecond;

        // 2. 점수 갱신
        UpdateScore();

        // 3. 죽음 조건 확인
        CheckDeathCondition();
    }

    /// <summary>
    /// 현재 수분 상태에 따라 점수를 계산하고 누적합니다.
    /// </summary>
    private void UpdateScore()
    {
        // 최적 함수량과의 거리를 계산 (0 ~ 1 사이의 값)
        float distanceFromOptimal = Mathf.Abs(CurrentWaterAmount - optimalWaterAmount) / waterRange;
        
        // 거리가 멀수록 점수가 낮아짐 (1 ~ 0 사이의 값)
        float qualityMultiplier = 1f - distanceFromOptimal;

        // 초당 얻는 점수는 품질에 비례 (최소 0점, 최대 1점)
        float pointsThisSecond = Mathf.Max(0, qualityMultiplier);
        
        CurrentScore += pointsThisSecond;
    }

    /// <summary>
    /// 수분량이 경계를 벗어났는지 확인하고 죽음을 처리합니다.
    /// </summary>
    private void CheckDeathCondition()
    {
        if (CurrentWaterAmount <= minWaterAmount || CurrentWaterAmount >= maxWaterAmount)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 작물에 물을 주는 함수 (외부에서 호출)
    /// </summary>
    /// <param name="amount">줄 물의 양</param>
    public void WaterCrop(float amount)
    {
        if (isDead) return;
        CurrentWaterAmount += amount;
    }

    /// <summary>
    /// 작물을 죽이고 죽은 작물 프리팹으로 교체합니다.
    /// </summary>
    private void Die()
    {
        isDead = true;
        CancelInvoke(nameof(UpdatePerSecond)); // 1초 반복 중단

        Debug.Log($"작물이 죽었습니다! 최종 점수: {CurrentScore:F2}");

        if (deadCropPrefab != null)
        {
            GameObject deadCrop = Instantiate(deadCropPrefab, transform.position, transform.rotation);
            deadCrop.transform.localScale = transform.localScale; // 현재 크기 유지
        }

        Destroy(gameObject);
    }
}