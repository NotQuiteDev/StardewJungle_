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
    [SerializeField] private GameObject normalStatusGroup;    // 슬라이더/텍스트 부모
    [SerializeField] private TextMeshProUGUI statusMessageText;

    [Header("일반 상태 UI")]
    [SerializeField] private Slider waterSlider;
    [SerializeField] private TextMeshProUGUI waterValueText;
    [SerializeField] private Slider growthSlider;
    [SerializeField] private TextMeshProUGUI growthPercentText;
    [SerializeField] private TextMeshProUGUI growthTimeText;
    [SerializeField] private RectTransform lowZoneDanger;     // (선택) 위험구간 바
    [SerializeField] private TextMeshProUGUI waterAmountText;
    [SerializeField] private RectTransform optimalZoneMarker; // 최적 위치 마커(세로선)
    [SerializeField] private RectTransform currentValueMarker;// 현재 위치 마커(세로선)

    [Header("마커/밴드")]
    [SerializeField] private RectTransform perfectZoneBand;   // ★ 그린존 띠(폭이 커짐)

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
                UpdateCropUI(crop);
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
        if (_mode == newMode) return; // 같은 모드면 아무 것도 안 함
        _mode = newMode;

        switch (_mode)
        {
            case TargetMode.Crop:
                normalStatusGroup.SetActive(true);
                statusMessageText.gameObject.SetActive(false);

                // Crop 전용 보조 UI ON
                if (growthSlider)       growthSlider.gameObject.SetActive(true);
                if (growthPercentText)  growthPercentText.gameObject.SetActive(true);
                if (growthTimeText)     growthTimeText.gameObject.SetActive(true);
                if (lowZoneDanger)      lowZoneDanger.gameObject.SetActive(true);
                if (optimalZoneMarker)  optimalZoneMarker.gameObject.SetActive(true);
                if (currentValueMarker) currentValueMarker.gameObject.SetActive(true);
                if (perfectZoneBand)    perfectZoneBand.gameObject.SetActive(true);
                break;

            case TargetMode.FarmPlot:
                normalStatusGroup.SetActive(true);
                statusMessageText.gameObject.SetActive(false);

                // FarmPlot은 수분/성장 UI 비활성(표시 최소화)
                if (growthSlider)       growthSlider.gameObject.SetActive(false);
                if (growthPercentText)  growthPercentText.gameObject.SetActive(false);
                if (growthTimeText)     growthTimeText.gameObject.SetActive(false);
                if (lowZoneDanger)      lowZoneDanger.gameObject.SetActive(false);
                if (optimalZoneMarker)  optimalZoneMarker.gameObject.SetActive(false);
                if (perfectZoneBand)    perfectZoneBand.gameObject.SetActive(false);
                if (currentValueMarker) currentValueMarker.gameObject.SetActive(true); // 현재값 마커는 재활용
                break;

            case TargetMode.None:
                // 그대로 두고 alpha만 0 처리
                break;
        }
    }

    // -------- Crop UI --------
    private void UpdateCropUI(CropManager crop)
    {
        if (crop.State == CropManager.CropState.Growing)
        {
            normalStatusGroup.SetActive(true);
            statusMessageText.gameObject.SetActive(false);

            // 정규화 값 패키지(0..1): pCur, pOpt, pL, pR
            crop.GetWaterUIData(out float pCur, out float pOpt, out float pL, out float pR);

            // 1) 슬라이더/텍스트
            if (waterSlider) waterSlider.value = pCur;
            if (waterValueText) waterValueText.text = $"Water: {pCur * 100f:F0}%";
            if (waterAmountText) waterAmountText.text = $"{crop.CurrentWaterAmount:F0} / {crop.MaxWaterAmount:F0}";

            // 2) 폭/위치 계산
            float w = (waterSliderRect != null) ? waterSliderRect.rect.width : 0f;

            // 위험 구간/최적 표시(선택)
            if (lowZoneDanger)
            {
                // min 구간: (Min / Max) 비율로 가로폭 지정 (원래 코드 유지)
                float minRatio = crop.MinWaterAmount / crop.MaxWaterAmount;
                var sd = lowZoneDanger.sizeDelta;
                sd.x = w * Mathf.Clamp01(minRatio);
                lowZoneDanger.sizeDelta = sd;
            }

            if (optimalZoneMarker)
                optimalZoneMarker.anchoredPosition = new Vector2(w * pOpt, optimalZoneMarker.anchoredPosition.y);

            if (currentValueMarker)
                currentValueMarker.anchoredPosition = new Vector2(w * pCur, currentValueMarker.anchoredPosition.y);

            // 3) 그린존 밴드(폭이 '비율만큼' 실제로 커짐)
            if (perfectZoneBand)
            {
                // 왼쪽 끝을 pL로 이동
                perfectZoneBand.anchoredPosition = new Vector2(w * pL, perfectZoneBand.anchoredPosition.y);
                // width = (pR - pL) * 전체 폭
                var sd = perfectZoneBand.sizeDelta;
                sd.x = Mathf.Max(0f, (pR - pL) * w);
                perfectZoneBand.sizeDelta = sd;
            }

            // 4) 성장 UI
            float growthPercent = crop.GrowthTimer / crop.GrowthDuration;
            if (growthSlider) growthSlider.value = growthPercent;
            if (growthPercentText) growthPercentText.text = $"Grown: {growthPercent * 100f:F0}%";
            if (growthTimeText)
            {
                float timeRemaining = Mathf.Max(0, crop.GrowthDuration - crop.GrowthTimer);
                int minutes = Mathf.FloorToInt(timeRemaining / 60);
                int seconds = Mathf.FloorToInt(timeRemaining % 60);
                growthTimeText.text = $"{minutes:00}:{seconds:00}";
            }
        }
        else if (crop.State == CropManager.CropState.Grown)
        {
            normalStatusGroup.SetActive(false);
            statusMessageText.gameObject.SetActive(true);
            statusMessageText.text = "Fully Grown\n(Press E/Game pad North to Harvest)";
        }
        else // Dead
        {
            normalStatusGroup.SetActive(false);
            statusMessageText.gameObject.SetActive(true);
            statusMessageText.text = "Dead\n(Press E/Game pad North to Clear)";
        }
    }

    // -------- FarmPlot UI --------
    private void UpdateFarmPlotUI(FarmPlot plot)
    {
        // water 슬라이더를 "Tilled %"로 재활용
        float tilled = plot.TilledPercentNormalized;
        if (waterSlider) waterSlider.value = tilled;

        if (waterValueText) waterValueText.text = $"Tilled: {tilled * 100f:F0}%";
        if (waterAmountText) waterAmountText.text = plot.IsFullyTilled ? "Fully tilled" : $"{Mathf.RoundToInt(tilled * 100f)}%";

        if (waterSliderRect != null && currentValueMarker != null)
        {
            float w = waterSliderRect.rect.width;
            currentValueMarker.anchoredPosition = new Vector2(w * tilled, currentValueMarker.anchoredPosition.y);
        }
    }
}