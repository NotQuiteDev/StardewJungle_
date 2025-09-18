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
}

