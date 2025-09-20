using UnityEngine;

// IInteractable 인터페이스를 따른다고 선언

[System.Serializable]
public struct HarvestGrade
{
    public string gradeName; // Inspector에서 알아보기 위한 이름 (예: Gold)
    public float minScoreRequired; // 이 등급을 받기 위한 최소 점수
    public HarvestedCropData cropItemToDrop; // 해당 등급일 때 드랍할 아이템
}
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

    [Header("초기 수분 설정 (심을 때)")]
    [Tooltip("체크하면 초기 수분을 Inspector 값으로 강제. 끄면 기본은 '최적 수분'에서 시작")]
    [SerializeField] private bool overrideInitialWater = false;
    [Tooltip("overrideInitialWater가 true일 때만 사용됨. [min, max] 범위로 자동 클램프")]
    [SerializeField] private float initialWaterAmountInspector = 50f;

    [Header("점수 튜닝 (Green Zone)")]
    [Tooltip("Min~Max 전체 범위 대비 '완벽 구간' 폭 비율 (0~1). 예: 0.2 = 전체의 20% 폭은 100점 처리")]
    [SerializeField, Range(0f, 1f)] private float greenZoneWidth01 = 0.20f;

    [Tooltip("그린존 밖 감쇠 곡선. 1=선형, 2=부드럽게(권장), 3~4=더 관대")]
    [SerializeField, Range(0.5f, 4f)] private float falloffExponent = 2.0f;

    // ## 2. Header("오브젝트 연결") 아래에 이 두 변수를 추가합니다. ##
    [Header("수확 및 드랍 설정")]
    [Tooltip("수확 시 생성될 '만능' 드랍 아이템 프리팹 (ItemDropShell)")]
    [SerializeField] private GameObject itemDropPrefab;

    [Tooltip("점수가 높은 순서대로(내림차순) 정렬해주세요!")]
    [SerializeField] private HarvestGrade[] harvestGrades;

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

    // 런타임으로 초기 수분 주입을 지원하기 위한 임시 저장소
    private bool hasPendingInitialWater = false;
    private float pendingInitialWater = 0f;

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

        // ★ 초기 수분 설정
        if (hasPendingInitialWater)
        {
            CurrentWaterAmount = Mathf.Clamp(pendingInitialWater, minWaterAmount, maxWaterAmount);
            hasPendingInitialWater = false; // 1회성 적용
        }
        else if (overrideInitialWater)
        {
            CurrentWaterAmount = Mathf.Clamp(initialWaterAmountInspector, minWaterAmount, maxWaterAmount);
        }
        else
        {
            CurrentWaterAmount = optimalWaterAmount; // 기본은 최적치에서 시작
        }
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

        // ## 수정된 부분: 디버그 로그 ##
        // =======================================================================
        _dbgTimer += Time.deltaTime;
        if (debugWaterEverySecond && _dbgTimer >= 1f)
        {
            _dbgTimer = 0f;

            // 핵심 정보 계산
            float grownPct = Mathf.Clamp01(growthTimer / Mathf.Max(1e-4f, growthDuration)) * 100f;
            float scoreAsOfNow = (qualityAccum / Mathf.Max(1e-4f, growthTimer)) * 100f;

            // 간결하게 로그 출력
            Debug.Log(
                $"[Crop] Water: {CurrentWaterAmount:F1} | " +
                $"Growth: {grownPct:F0}% ({growthTimer:F1}s) | " +
                $"Score(Now): {scoreAsOfNow:F1}",
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
        float min = minWaterAmount;
        float max = maxWaterAmount;
        float opt = optimalWaterAmount;

        float span = Mathf.Max(1e-4f, max - min);
        float left = opt - min;      // 최적→왼쪽 경계 거리
        float right = max - opt;     // 최적→오른쪽 경계 거리

        float greenHalf = Mathf.Clamp01(greenZoneWidth01) * 0.5f * span;

        float delta = Mathf.Abs(CurrentWaterAmount - opt);

        float quality; // 0..1

        if (delta <= greenHalf)
        {
            quality = 1f;
        }
        else
        {
            float over = delta - greenHalf;
            float denom = (CurrentWaterAmount >= opt)
                ? Mathf.Max(1e-4f, right - greenHalf)
                : Mathf.Max(1e-4f, left - greenHalf);

            float t = Mathf.Clamp01(over / denom);
            quality = 1f - Mathf.Pow(t, falloffExponent);
        }

        qualityAccum += quality * Time.deltaTime;
        CurrentScore = Mathf.Clamp01(qualityAccum / Mathf.Max(1e-4f, growthDuration)) * 100f;
    }

    private void CheckDeathCondition()
    {
        // 물이 최소치보다 적거나 같을 때만 죽도록 조건을 변경한다.
        if (CurrentWaterAmount <= minWaterAmount)
        {
            Die();
        }
    }

    public void WaterCrop(float amount)
    {
        if (State == CropState.Growing)
        {
            CurrentWaterAmount += amount;
            // 물을 준 후에, 최대 수분량을 넘지 않도록 값을 제한(Clamp)한다.
            CurrentWaterAmount = Mathf.Min(CurrentWaterAmount, maxWaterAmount);
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
 // CropManager.cs 의 Interact() 함수
    public void Interact()
    {
        var plot = GetComponentInParent<FarmPlot>();

        switch (State)
        {
            case CropState.Grown:
                if (plot != null) plot.OnHarvestedReduceToHalf();

                // ## 수확 로직 시작 ##
                float finalScore = CurrentScorePercent;
                HarvestedCropData itemToDrop = null;

                // 점수가 높은 순으로 정렬된 등급 배열을 순회
                foreach (var grade in harvestGrades)
                {
                    if (finalScore >= grade.minScoreRequired)
                    {
                        itemToDrop = grade.cropItemToDrop;
                        break; // 가장 먼저 조건을 만족하는 최상위 등급을 선택하고 종료
                    }
                }

                // 적절한 등급의 아이템을 찾았다면 드랍
                if (itemToDrop != null)
                {
                    Debug.Log($"작물을 수확했습니다! 최종 점수: {Mathf.FloorToInt(finalScore)}점, 등급: {itemToDrop.itemName}");

                    if (itemDropPrefab != null)
                    {
                        // ItemDropShell 프리팹을 작물의 위치에 생성
                        GameObject drop = Instantiate(itemDropPrefab, transform.position, Quaternion.identity);
                        // 어떤 아이템인지, 몇 개인지 정보를 주입
                        drop.GetComponent<ItemDrop>().Initialize(itemToDrop, 1); // 1개 드랍
                    }
                }
                else
                {
                    Debug.LogWarning("수확 가능한 아이템이 없습니다. harvestGrades 설정을 확인하세요.");
                }
                
                Destroy(gameObject);
                break;

            case CropState.Dead:
                if (plot != null) plot.OnHarvestedReduceToHalf();
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
            case CropState.Dead: return "Clear";
            default: return "";
        }
    }

    // ===== Runtime API: 심기 직전 원하는 초기 수분 주입 =====

    /// <summary>
    /// 절대 수치로 초기 수분을 지정(심기 직전 호출 권장). [min,max]로 자동 클램프.
    /// </summary>
    public void SetInitialWaterAbsolute(float waterAmount)
    {
        pendingInitialWater = waterAmount;
        hasPendingInitialWater = true;
    }

    /// <summary>
    /// 0..1 정규화 비율로 초기 수분을 지정. 0=min, 1=max.
    /// </summary>
    public void SetInitialWaterNormalized(float t01)
    {
        t01 = Mathf.Clamp01(t01);
        float abs = Mathf.Lerp(minWaterAmount, maxWaterAmount, t01);
        SetInitialWaterAbsolute(abs);
    }
}
