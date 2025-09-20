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
    [SerializeField] private GameObject normalStatusGroup;    // 공통 부모
    [SerializeField] private TextMeshProUGUI statusMessageText;

    [Header("Crop: 성장 UI")]
    [SerializeField] private Slider growthSlider;              // 성장 슬라이더

    [Header("FarmPlot: Tilled UI")]
    [SerializeField] private Slider tilledSlider;              // 경작도 슬라이더

    [Header("라벨 표시")]
    [SerializeField] private TextMeshProUGUI modeLabelText;    // "Growth" 또는 "Tilled" 라벨

    private CanvasGroup statusCanvasGroup;

    private enum TargetMode { None, Crop, FarmPlot }
    private TargetMode _mode = TargetMode.None;

    private void Awake()
    {
        if (raycastCamera == null) raycastCamera = Camera.main;
        statusCanvasGroup = statusWindowGroup.GetComponent<CanvasGroup>();
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
        if (_mode == newMode) return;
        _mode = newMode;

        switch (_mode)
        {
            case TargetMode.Crop:
                normalStatusGroup.SetActive(true);
                statusMessageText.gameObject.SetActive(false);

                if (growthSlider) growthSlider.gameObject.SetActive(true);
                if (tilledSlider) tilledSlider.gameObject.SetActive(false);
                if (modeLabelText) modeLabelText.gameObject.SetActive(true);
                break;

            case TargetMode.FarmPlot:
                normalStatusGroup.SetActive(true);
                statusMessageText.gameObject.SetActive(false);

                if (growthSlider) growthSlider.gameObject.SetActive(false);
                if (tilledSlider) tilledSlider.gameObject.SetActive(true);
                if (modeLabelText) modeLabelText.gameObject.SetActive(true);
                break;

            case TargetMode.None:
                // 그냥 alpha=0 처리
                break;
        }
    }

    // -------- Crop --------
    private void UpdateCropUI(CropManager crop)
    {
        if (crop.State == CropManager.CropState.Growing)
        {
            normalStatusGroup.SetActive(true);
            statusMessageText.gameObject.SetActive(false);

            float growthPercent = crop.GrowthTimer / crop.GrowthDuration;
            if (growthSlider) growthSlider.value = growthPercent;

            if (modeLabelText) modeLabelText.text = "Growth";
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

    // -------- FarmPlot --------
    private void UpdateFarmPlotUI(FarmPlot plot)
    {
        float tilled = Mathf.Clamp01(plot.TilledPercentNormalized);

        if (tilledSlider) tilledSlider.value = tilled;

        if (modeLabelText) modeLabelText.text = "Tilled";
    }
}
