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
    [SerializeField] private Slider waterSlider;
    [SerializeField] private TextMeshProUGUI waterValueText;
    [SerializeField] private Slider growthSlider;
    [SerializeField] private TextMeshProUGUI growthPercentText;
    [SerializeField] private TextMeshProUGUI growthTimeText;

    [Header("상태창 위험/최적 구간 연결")]
    [Tooltip("수분 슬라이더의 낮은 수분 위험 구간(빨간 패널)을 연결하세요.")]
    [SerializeField] private RectTransform lowZoneDanger;
    [Tooltip("슬라이더 위에 현재 수분량을 표시할 텍스트를 연결하세요.")]
    [SerializeField] private TextMeshProUGUI waterAmountText;
    [Tooltip("수분 슬라이더의 최적 지점 마커(초록 막대)를 연결하세요.")] // ▼▼▼ 추가된 부분 ▼▼▼
    [SerializeField] private RectTransform optimalZoneMarker; // ▼▼▼ 추가된 부분 ▼▼▼
    [Tooltip("현재 수치를 표시할 커스텀 마커를 연결하세요.")] // ▼▼▼ 추가된 부분 ▼▼▼
    [SerializeField] private RectTransform currentValueMarker; 

    private CanvasGroup statusCanvasGroup;
    private RectTransform waterSliderRect;

    private void Awake()
    {
        if (raycastCamera == null) raycastCamera = Camera.main;
        
        statusCanvasGroup = statusWindowGroup.GetComponent<CanvasGroup>();
        if (waterSlider != null)
        {
            waterSliderRect = waterSlider.GetComponent<RectTransform>();
        }
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
            CropManager crop = hit.collider.GetComponentInParent<CropManager>();
            if (crop != null)
            {
                statusCanvasGroup.alpha = 1f;
                UpdateStatusUI(crop);
                return;
            }
        }

        statusCanvasGroup.alpha = 0f;
    }

    private void UpdateStatusUI(CropManager crop)
    {
        // --- 수분 UI 업데이트 ---
        float waterPercent = crop.CurrentWaterAmount / crop.MaxWaterAmount;
        waterSlider.value = waterPercent;
        waterValueText.text = $"Water: {waterPercent * 100f:F0}%";
        
        if (waterAmountText != null)
        {
            waterAmountText.text = $"{crop.CurrentWaterAmount:F0} / {crop.MaxWaterAmount:F0}";
        }
        
        if (lowZoneDanger != null && waterSliderRect != null)
        {
            float minPercent = crop.MinWaterAmount / crop.MaxWaterAmount;
            float sliderWidth = waterSliderRect.rect.width;
            lowZoneDanger.sizeDelta = new Vector2(sliderWidth * minPercent, lowZoneDanger.sizeDelta.y);
        }

        // ▼▼▼ 추가된 부분: 최적 지점 마커 위치 계산 ▼▼▼
        if (optimalZoneMarker != null && waterSliderRect != null)
        {
            float optimalPercent = crop.OptimalWaterAmount / crop.MaxWaterAmount;
            float sliderWidth = waterSliderRect.rect.width;
            optimalZoneMarker.anchoredPosition = new Vector2(sliderWidth * optimalPercent, optimalZoneMarker.anchoredPosition.y);
        }
        if (currentValueMarker != null && waterSliderRect != null)
        {
            float sliderWidth = waterSliderRect.rect.width;
            // waterPercent 값은 위에서 이미 계산됨
            currentValueMarker.anchoredPosition = new Vector2(sliderWidth * waterPercent, currentValueMarker.anchoredPosition.y);
        }
        // ▲▲▲ 추가된 부분 ▲▲▲

        // --- 성장 UI 업데이트 (이하 동일) ---
        float growthPercent = crop.GrowthTimer / crop.GrowthDuration;
        growthSlider.value = growthPercent;
        growthPercentText.text = $"Grown: {growthPercent * 100f:F0}%";
        float timeRemaining = crop.GrowthDuration - crop.GrowthTimer;
        timeRemaining = Mathf.Max(0, timeRemaining);
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        growthTimeText.text = $"{minutes:00}:{seconds:00}";
    }
}