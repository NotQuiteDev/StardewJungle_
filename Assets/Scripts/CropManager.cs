using UnityEngine;

// IInteractable 인터페이스를 따른다고 선언
public class CropManager : MonoBehaviour, IInteractable
{
    public enum CropState { Growing, Grown, Dead }
    public CropState State { get; private set; }

    [Header("오브젝트 연결")]
    [SerializeField] private Transform cropVisuals;

    [Header("성장 설정")]
    [SerializeField] private float growthDuration = 60f;
    [SerializeField] private Vector3 startScale = new Vector3(0.1f, 0.1f, 0.1f);
    [SerializeField] private Vector3 maxScale = Vector3.one;

    [Header("수분 설정")]
    [SerializeField] private float minWaterAmount = 10f;
    [SerializeField] private float maxWaterAmount = 100f;
    [SerializeField] private float waterLossPerSecond = 1f;

    [Header("최적 수분 설정")]
    [Tooltip("체크하면 아래 값으로 최적 수분을 고정, 아니면 (min+max)/2 사용")]
    [SerializeField] private bool overrideOptimalWater = false;
    [Tooltip("overrideOptimalWater가 true일 때만 사용됨. [min, max] 범위로 자동 클램프")]
    [SerializeField] private float optimalWaterAmountInspector = 50f;

    // --- Public Properties ---
    public float CurrentWaterAmount { get; private set; }
    public float CurrentScore { get; private set; } // 0~100 스케일로 동기화
    public float MaxWaterAmount => maxWaterAmount;
    public float MinWaterAmount => minWaterAmount;
    public float GrowthDuration => growthDuration;
    public float GrowthTimer => growthTimer;
    public float OptimalWaterAmount => optimalWaterAmount;

    // --- Private Variables ---
    private float optimalWaterAmount;
    private float normRangeFromOptimal; // 정규화 기준 거리(최적→가까운 경계까지)
    private float growthTimer;
    private Renderer[] visualRenderers;

    [Header("품질 점수(정규화)")]
    [SerializeField] private float qualityAccum = 0f; // quality 적분(초 단위)

    public float CurrentScorePercent
        => Mathf.Clamp01(qualityAccum / Mathf.Max(1e-4f, growthDuration)) * 100f;

    private void Awake()
    {
        if (cropVisuals == null)
        {
            Debug.LogError("Crop Visuals가 연결되지 않았습니다!", this.gameObject);
            State = CropState.Dead;
            return;
        }
        visualRenderers = cropVisuals.GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        State = CropState.Growing;
        cropVisuals.localScale = startScale;

        // 최적 수분 결정
        float mid = (minWaterAmount + maxWaterAmount) * 0.5f;
        optimalWaterAmount = overrideOptimalWater
            ? Mathf.Clamp(optimalWaterAmountInspector, minWaterAmount, maxWaterAmount)
            : mid;

        // 정규화 기준 거리: 최적치에서 가까운 경계까지의 거리
        normRangeFromOptimal = Mathf.Max(1e-4f,
            Mathf.Min(optimalWaterAmount - minWaterAmount, maxWaterAmount - optimalWaterAmount));

        CurrentWaterAmount = optimalWaterAmount; // 시작은 최적치
    }

    private void Update()
    {
        if (State != CropState.Growing) return;

        // 성장
        if (growthTimer < growthDuration)
        {
            growthTimer += Time.deltaTime;
            cropVisuals.localScale = Vector3.Lerp(startScale, maxScale, growthTimer / growthDuration);
        }
        else
        {
            State = CropState.Grown;
        }

        // 수분 감소 + 점수
        CurrentWaterAmount -= waterLossPerSecond * Time.deltaTime;
        UpdateScore();
        CheckDeathCondition();
    }

    private void UpdateScore()
    {
        // 0=최적, 1=허용 경계에 닿음(가장 가까운 경계 기준)
        float deviation = Mathf.Abs(CurrentWaterAmount - optimalWaterAmount) / normRangeFromOptimal;
        float quality = Mathf.Clamp01(1f - deviation); // 최적일수록 1

        qualityAccum += quality * Time.deltaTime;
        CurrentScore = CurrentScorePercent; // 0~100 동기화
    }

    private void CheckDeathCondition()
    {
        if (CurrentWaterAmount <= minWaterAmount || CurrentWaterAmount >= maxWaterAmount)
        {
            Die();
        }
    }

    public void WaterCrop(float amount)
    {
        if (State == CropState.Growing)
        {
            CurrentWaterAmount += amount;
        }
    }

    private void Die()
    {
        State = CropState.Dead;
        CurrentWaterAmount = 0;
        growthTimer = 0;

        foreach (var rend in visualRenderers)
        {
            rend.material.color = Color.black;
        }
    }

    // 수확/제거 둘 다 플롯 50% 강등
    public void Interact()
    {
        var plot = GetComponentInParent<FarmPlot>();

        switch (State)
        {
            case CropState.Grown:
                if (plot != null) plot.OnHarvestedReduceToHalf();
                Debug.Log($"작물을 수확했습니다! 최종 점수: {Mathf.FloorToInt(CurrentScorePercent)}점");
                Destroy(gameObject);
                break;

            case CropState.Dead:
                if (plot != null) plot.OnHarvestedReduceToHalf(); // ★ 죽은 작물 제거도 50%
                Debug.Log("죽은 작물을 제거했습니다.");
                Destroy(gameObject);
                break;

            case CropState.Growing:
                // 자라는 중엔 무시
                break;
        }
    }

    public string GetInteractionText()
    {
        switch (State)
        {
            case CropState.Grown: return "Harvest";
            case CropState.Dead:  return "Clear";
            default:              return "";
        }
    }
}
