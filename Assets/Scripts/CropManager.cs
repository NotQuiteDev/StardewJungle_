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


    [Header("점수 튜닝 (Green Zone)")]
    [Tooltip("Min~Max 전체 범위 대비 '완벽 구간' 폭 비율 (0~1). 예: 0.2 = 전체의 20% 폭은 100점 처리")]
    [SerializeField, Range(0f, 1f)] private float greenZoneWidth01 = 0.20f;

    [Tooltip("그린존 밖 감쇠 곡선. 1=선형, 2=부드럽게(권장), 3~4=더 관대")]
    [SerializeField, Range(0.5f, 4f)] private float falloffExponent = 2.0f;

    // UI/외부에서 읽기용
    public float GreenZoneWidth01 => greenZoneWidth01;
    public float FalloffExponent => falloffExponent;

    // 1초 디버그
    [SerializeField] private bool debugWaterEverySecond = false;
    private float _dbgTimer = 0f;

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


        // --- 맨 아래에 추가 ---
        _dbgTimer += Time.deltaTime;
        if (debugWaterEverySecond && _dbgTimer >= 1f)
        {
            _dbgTimer = 0f;

            // 정규화 값 계산(0..1)
            float pMin = 0f;
            float pMax = 1f;
            float pCur = Mathf.Clamp01(Mathf.InverseLerp(minWaterAmount, maxWaterAmount, CurrentWaterAmount));
            float pOpt = Mathf.Clamp01(Mathf.InverseLerp(minWaterAmount, maxWaterAmount, optimalWaterAmount));

            // 그린존 좌/우 경계(정규화)
            float half = Mathf.Clamp01(greenZoneWidth01) * 0.5f;
            float pL = Mathf.Clamp01(pOpt - half);
            float pR = Mathf.Clamp01(pOpt + half);

            Debug.Log(
                $"[CropDebug] Water: {CurrentWaterAmount:F1} (norm {pCur:F2}) | " +
                $"Min/Max: {minWaterAmount}/{maxWaterAmount} | " +
                $"Optimal: {optimalWaterAmount} (norm {pOpt:F2}) | " +
                $"Green[{pL:F2}..{pR:F2}] width={greenZoneWidth01:P0} | " +
                $"Score={CurrentScore:F1}",
                this
            );
        }
    }
    /// <summary>
    /// UI가 바로 쓸 수 있는 정규화 값(0..1)들 반환:
    /// pCur=현재, pOpt=최적, pL/pR=그린존 좌/우.
    /// </summary>
    public void GetWaterUIData(out float pCur, out float pOpt, out float pL, out float pR)
    {
        pCur = Mathf.Clamp01(Mathf.InverseLerp(minWaterAmount, maxWaterAmount, CurrentWaterAmount));
        pOpt = Mathf.Clamp01(Mathf.InverseLerp(minWaterAmount, maxWaterAmount, optimalWaterAmount));
        float half = Mathf.Clamp01(greenZoneWidth01) * 0.5f;
        pL = Mathf.Clamp01(pOpt - half);
        pR = Mathf.Clamp01(pOpt + half);
    }

    private void UpdateScore()
    {
        // 범위/최적치 파생값
        float min = minWaterAmount;
        float max = maxWaterAmount;
        float opt = optimalWaterAmount;

        float span = Mathf.Max(1e-4f, max - min);
        float left = opt - min; // 최적→왼쪽 경계 거리
        float right = max - opt; // 최적→오른쪽 경계 거리

        // 그린존(완벽 구간) 절반폭: 전체 범위 비율 * 0.5
        float greenHalf = Mathf.Clamp01(greenZoneWidth01) * 0.5f * span;

        // 현재 값이 최적에서 얼마나 떨어졌는가
        float delta = Mathf.Abs(CurrentWaterAmount - opt);

        float quality; // 0~1

        if (delta <= greenHalf)
        {
            // 그린존 내부: 100% 품질
            quality = 1f;
        }
        else
        {
            // 그린존 밖: 초과량을 측면별 실제 남은 거리로 정규화해 감쇠
            float over = delta - greenHalf;

            // 오른쪽/왼쪽 측면별 유효 분모(그린존 제외 후 남은 거리)
            float denom = (CurrentWaterAmount >= opt)
                ? Mathf.Max(1e-4f, right - greenHalf)
                : Mathf.Max(1e-4f, left - greenHalf);

            // 0..1: 그린존 경계에서 실제 경계까지
            float t = Mathf.Clamp01(over / denom);

            // 감쇠 곡선: 1 - t^e  (e가 클수록 초반 완만, 더 관대)
            quality = 1f - Mathf.Pow(t, falloffExponent);
        }

        // 시간 적분(성장 중에만 호출됨) → 0~100으로 정규화
        qualityAccum += quality * Time.deltaTime;
        CurrentScore = Mathf.Clamp01(qualityAccum / Mathf.Max(1e-4f, growthDuration)) * 100f;
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
