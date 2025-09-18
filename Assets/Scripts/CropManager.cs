using UnityEngine;

// IInteractable 인터페이스를 따른다고 선언
public class CropManager : MonoBehaviour, IInteractable
{
    // 작물의 현재 상태를 관리하기 위한 열거형(enum)
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
    
    // --- Public Properties (외부 접근용) ---
    public float CurrentWaterAmount { get; private set; }
    public float CurrentScore { get; private set; }
    public float MaxWaterAmount => maxWaterAmount;
    public float MinWaterAmount => minWaterAmount;
    public float GrowthDuration => growthDuration;
    public float GrowthTimer => growthTimer;
    public float OptimalWaterAmount => optimalWaterAmount;

    // --- Private Variables (내부 변수) ---
    private float optimalWaterAmount;
    private float waterRange;
    private float growthTimer;
    private Renderer[] visualRenderers; // 자식의 모든 렌더러를 저장할 배열

    private void Awake()
    {
        if (cropVisuals == null)
        {
            Debug.LogError("Crop Visuals가 연결되지 않았습니다!", this.gameObject);
            State = CropState.Dead; // 에러 시 즉시 죽음 처리
            return;
        }
        // 자식 오브젝트의 모든 렌더러를 미리 찾아 저장 (최적화)
        visualRenderers = cropVisuals.GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        State = CropState.Growing;
        cropVisuals.localScale = startScale;
        optimalWaterAmount = (minWaterAmount + maxWaterAmount) / 2f;
        waterRange = (maxWaterAmount - minWaterAmount) / 2f;
        CurrentWaterAmount = optimalWaterAmount;
    }

    private void Update()
    {
        // 자라는 중일 때만 아래 로직을 실행
        if (State != CropState.Growing) return;

        // 성장 처리
        if (growthTimer < growthDuration)
        {
            growthTimer += Time.deltaTime;
            cropVisuals.localScale = Vector3.Lerp(startScale, maxScale, growthTimer / growthDuration);
        }
        else
        {
            // 성장이 완료되면 상태 변경
            State = CropState.Grown;
        }

        // 수분 및 점수 처리
        CurrentWaterAmount -= waterLossPerSecond * Time.deltaTime;
        UpdateScore();
        CheckDeathCondition();
    }

    private void UpdateScore()
    {
        float distanceFromOptimal = Mathf.Abs(CurrentWaterAmount - optimalWaterAmount) / waterRange;
        float qualityMultiplier = 1f - distanceFromOptimal;
        CurrentScore += Mathf.Max(0, qualityMultiplier) * Time.deltaTime;
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
        // 자라는 중일 때만 물을 줄 수 있음
        if (State == CropState.Growing)
        {
            CurrentWaterAmount += amount;
        }
    }

    // ▼▼▼ 핵심 수정: Die() 함수 ▼▼▼
    private void Die()
    {
        State = CropState.Dead;
        CurrentWaterAmount = 0; // UI 표시를 위해 0으로 설정
        growthTimer = 0;
        
        // 모든 비주얼 파츠의 색상을 검정으로 변경
        foreach (var rend in visualRenderers)
        {
            rend.material.color = Color.black;
        }
    }

    // ▼▼▼ 핵심 추가: IInteractable 인터페이스 구현 ▼▼▼
    public void Interact()
    {
        switch (State)
        {
            case CropState.Grown:
                Debug.Log($"작물을 수확했습니다! 최종 점수: {Mathf.FloorToInt(CurrentScore)}점");
                Destroy(gameObject);
                break;
            case CropState.Dead:
                Debug.Log("죽은 작물을 제거했습니다.");
                Destroy(gameObject);
                break;
            case CropState.Growing:
                // 자라는 중에는 아무것도 하지 않음
                break;
        }
    }

    public string GetInteractionText()
    {
        switch (State)
        {
            case CropState.Grown:
                return "Harvest";
            case CropState.Dead:
                return "Clear";
            default:
                return "";
        }
    }
}
