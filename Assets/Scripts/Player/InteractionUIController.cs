using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUIController : MonoBehaviour
{
    [Header("레이캐스트 설정")]
    [SerializeField] private Camera raycastCamera;
    [SerializeField] private float raycastDistance = 5f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI 요소 연결")]
    [SerializeField] private GameObject statusWindowGroup;
    [SerializeField] private GameObject normalStatusGroup; // 슬라이더, 퍼센트 텍스트 등을 담는 부모 그룹
    [SerializeField] private TextMeshProUGUI statusMessageText; // ▼▼▼ 새로 추가된 상태 메시지 텍스트 ▼▼▼

    [Header("일반 상태 UI")]
    [SerializeField] private Slider waterSlider;
    [SerializeField] private TextMeshProUGUI waterValueText;
    [SerializeField] private Slider growthSlider;
    [SerializeField] private TextMeshProUGUI growthPercentText;
    [SerializeField] private TextMeshProUGUI growthTimeText;
    [SerializeField] private RectTransform lowZoneDanger;
    [SerializeField] private TextMeshProUGUI waterAmountText;
    [SerializeField] private RectTransform optimalZoneMarker;
    [SerializeField] private RectTransform currentValueMarker;

    private CanvasGroup statusCanvasGroup;
    private RectTransform waterSliderRect;
    private enum TargetMode { None, Crop, FarmPlot }
    private TargetMode _mode = TargetMode.None;

    private void Awake()
    {
        if (raycastCamera == null) raycastCamera = Camera.main;
        statusCanvasGroup = statusWindowGroup.GetComponent<CanvasGroup>();
        if (waterSlider != null) waterSliderRect = waterSlider.GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (statusCanvasGroup != null) statusCanvasGroup.alpha = 0f;
    }



    private void Update()
    {
        if (raycastCamera == null || statusCanvasGroup == null) return;

        Ray ray = new Ray(raycastCamera.transform.position, raycastCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactableLayer))
        {
            // 1) Crop 우선
            CropManager crop = hit.collider.GetComponentInParent<CropManager>();
            if (crop != null)
            {
                SetMode(TargetMode.Crop);
                statusCanvasGroup.alpha = 1f;
                UpdateStatusUI(crop);
                return;
            }

            // 2) FarmPlot 다음
            FarmPlot plot = hit.collider.GetComponentInParent<FarmPlot>();
            if (plot != null)
            {
                SetMode(TargetMode.FarmPlot);
                statusCanvasGroup.alpha = 1f;
                UpdateFarmPlotUI(plot);
                return;
            }
        }

        SetMode(TargetMode.None);
        statusCanvasGroup.alpha = 0f;
    }

    private void SetMode(TargetMode newMode)
    {
        if (_mode == newMode) return; // 같은 모드면 아무 것도 안 함 (토글 GC 방지)
        _mode = newMode;

        switch (_mode)
        {
            case TargetMode.Crop:
                normalStatusGroup.SetActive(true);
                statusMessageText.gameObject.SetActive(false);

                // Crop 전용 보조 UI ON
                growthSlider.gameObject.SetActive(true);
                growthPercentText.gameObject.SetActive(true);
                growthTimeText.gameObject.SetActive(true);
                lowZoneDanger.gameObject.SetActive(true);
                optimalZoneMarker.gameObject.SetActive(true);
                currentValueMarker.gameObject.SetActive(true);
                break;

            case TargetMode.FarmPlot:
                normalStatusGroup.SetActive(true);
                statusMessageText.gameObject.SetActive(false);

                // FarmPlot은 “수분/성장” 안 씀 → 관련 UI OFF (한 번만)
                growthSlider.gameObject.SetActive(false);
                growthPercentText.gameObject.SetActive(false);
                growthTimeText.gameObject.SetActive(false);
                lowZoneDanger.gameObject.SetActive(false);
                optimalZoneMarker.gameObject.SetActive(false);
                // currentValueMarker는 위치표시로 재활용
                currentValueMarker.gameObject.SetActive(true);
                break;

            case TargetMode.None:
                // 필요시 그룹을 유지하고 alpha만 0으로 해도 됨
                break;
        }
    }

    // ▼▼▼ 최종 수정된 UpdateStatusUI() 함수 ▼▼▼
    private void UpdateStatusUI(CropManager crop)
    {
        switch (crop.State)
        {
            case CropManager.CropState.Growing:
                normalStatusGroup.SetActive(true);
                statusMessageText.gameObject.SetActive(false); // 상태 메시지는 끈다

                // --- 수분 UI 업데이트 ---
                float waterPercent = crop.CurrentWaterAmount / crop.MaxWaterAmount;
                waterSlider.value = waterPercent;
                waterValueText.text = $"Water: {waterPercent * 100f:F0}%";
                waterAmountText.text = $"{crop.CurrentWaterAmount:F0} / {crop.MaxWaterAmount:F0}";

                // 마커 및 위험 구간 업데이트
                float sliderWidth = waterSliderRect.rect.width;
                lowZoneDanger.sizeDelta = new Vector2(sliderWidth * (crop.MinWaterAmount / crop.MaxWaterAmount), lowZoneDanger.sizeDelta.y);
                optimalZoneMarker.anchoredPosition = new Vector2(sliderWidth * (crop.OptimalWaterAmount / crop.MaxWaterAmount), optimalZoneMarker.anchoredPosition.y);
                currentValueMarker.anchoredPosition = new Vector2(sliderWidth * waterPercent, currentValueMarker.anchoredPosition.y);

                // --- 성장 UI 업데이트 ---
                float growthPercent = crop.GrowthTimer / crop.GrowthDuration;
                growthSlider.value = growthPercent;
                growthPercentText.text = $"Grown: {growthPercent * 100f:F0}%";
                float timeRemaining = crop.GrowthDuration - crop.GrowthTimer;
                timeRemaining = Mathf.Max(0, timeRemaining);
                int minutes = Mathf.FloorToInt(timeRemaining / 60);
                int seconds = Mathf.FloorToInt(timeRemaining % 60);
                growthTimeText.text = $"{minutes:00}:{seconds:00}";
                break;

            case CropManager.CropState.Grown:
                normalStatusGroup.SetActive(false); // 일반 정보 UI 끄기
                statusMessageText.gameObject.SetActive(true); // 상태 메시지 켜기
                statusMessageText.text = "Fully Grown\n(Press E/Game pad North to Harvest)";
                break;

            case CropManager.CropState.Dead:
                normalStatusGroup.SetActive(false); // 일반 정보 UI 끄기
                statusMessageText.gameObject.SetActive(true); // 상태 메시지 켜기
                statusMessageText.text = "Dead\n(Press E/Game pad North to Clear)";
                break;
        }
    }
    private void UpdateFarmPlotUI(FarmPlot plot)
    {
        // water 슬라이더를 "Tilled %"로 재활용
        float tilled = plot.TilledPercentNormalized;
        waterSlider.value = tilled;

        // 라벨/텍스트 재활용
        waterValueText.text = $"Tilled: {tilled * 100f:F0}%";
        waterAmountText.text = plot.IsFullyTilled ? "Fully tilled" : $"{Mathf.RoundToInt(tilled * 100f)}%";

        // 마커 위치(옵션): 현재값 마커만 움직임
        if (waterSliderRect != null && currentValueMarker != null)
        {
            float w = waterSliderRect.rect.width;
            currentValueMarker.anchoredPosition = new Vector2(w * tilled, currentValueMarker.anchoredPosition.y);
        }

        // 100%면 수치 숨기고 메시지로 바꾸고 싶다면:
        // if (plot.IsFullyTilled) {
        //     statusMessageText.gameObject.SetActive(true);
        //     statusMessageText.text = "Tilled Soil";
        //     normalStatusGroup.SetActive(false);
        // }
    }
}

