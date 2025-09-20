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
    [SerializeField] private Slider tilledSlider;              // 경작도 슬라이더 (0..1)

    [Header("라벨 표시")]
    [SerializeField] private TextMeshProUGUI modeLabelText;    // "Growth" 또는 "Tilled"

    private CanvasGroup statusCanvasGroup;

    private enum TargetMode { None, Crop, FarmPlot, Bed, GenericNPC }
    private TargetMode _mode = TargetMode.None;

    private void Awake()
    {
        if (raycastCamera == null) raycastCamera = Camera.main;

        if (statusWindowGroup != null)
            statusCanvasGroup = statusWindowGroup.GetComponent<CanvasGroup>();

        // 안전 장치: CanvasGroup이 없으면 추가
        if (statusCanvasGroup == null && statusWindowGroup != null)
            statusCanvasGroup = statusWindowGroup.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (statusCanvasGroup != null) statusCanvasGroup.alpha = 0f;
    }

    private void Update()
    {
        if (raycastCamera == null || statusCanvasGroup == null) return;

        // 화면 정중앙에서 전방 레이
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

            // 3) Bed
            Bed bed = hit.collider.GetComponentInParent<Bed>();
            if (bed != null)
            {
                SetMode(TargetMode.Bed);
                statusCanvasGroup.alpha = 1f;
                UpdateBedUI(bed);
                return;
            }
            InteractableNPC npc = hit.collider.GetComponentInParent<InteractableNPC>();
            if (npc != null)
            {
                SetMode(TargetMode.GenericNPC);
                statusCanvasGroup.alpha = 1f;
                UpdateGenericNPCUI(npc); // 새로 만든 함수 호출
                return;
            }
        }

        // 감지 대상 없음
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
                if (normalStatusGroup) normalStatusGroup.SetActive(true);
                if (statusMessageText) statusMessageText.gameObject.SetActive(false);

                if (growthSlider) growthSlider.gameObject.SetActive(true);
                if (tilledSlider) tilledSlider.gameObject.SetActive(false);
                if (modeLabelText) modeLabelText.gameObject.SetActive(true);
                break;

            case TargetMode.FarmPlot:
                if (normalStatusGroup) normalStatusGroup.SetActive(true);
                if (statusMessageText) statusMessageText.gameObject.SetActive(false);

                if (growthSlider) growthSlider.gameObject.SetActive(false);
                if (tilledSlider) tilledSlider.gameObject.SetActive(true);
                if (modeLabelText) modeLabelText.gameObject.SetActive(true);
                break;

            case TargetMode.Bed:
                if (normalStatusGroup) normalStatusGroup.SetActive(false); // 슬라이더류 OFF
                if (statusMessageText) statusMessageText.gameObject.SetActive(true);
                if (modeLabelText) modeLabelText.gameObject.SetActive(false);
                if (growthSlider) growthSlider.gameObject.SetActive(false);
                if (tilledSlider) tilledSlider.gameObject.SetActive(false);
                break;
            // ## 추가 ## : 범용 NPC를 위한 UI 레이아웃 설정 (Bed와 동일)
            case TargetMode.GenericNPC:
                if (normalStatusGroup) normalStatusGroup.SetActive(false); // 슬라이더 그룹 끄기
                if (statusMessageText) statusMessageText.gameObject.SetActive(true); // 메시지 텍스트 켜기
                if (modeLabelText) modeLabelText.gameObject.SetActive(false);
                if (growthSlider) growthSlider.gameObject.SetActive(false);
                if (tilledSlider) tilledSlider.gameObject.SetActive(false);
                break;

            case TargetMode.None:
                // 그대로 두고 alpha만 0 처리 (Update에서 제어)
                break;
        }
    }

    // -------- Crop --------
    private void UpdateCropUI(CropManager crop)
    {
        if (crop.State == CropManager.CropState.Growing)
        {
            if (normalStatusGroup) normalStatusGroup.SetActive(true);
            if (statusMessageText) statusMessageText.gameObject.SetActive(false);

            float growthPercent = (crop.GrowthDuration > 0f)
                ? Mathf.Clamp01(crop.GrowthTimer / crop.GrowthDuration)
                : 0f;

            if (growthSlider) growthSlider.value = growthPercent;
            if (modeLabelText) modeLabelText.text = "Growth";
        }
        else if (crop.State == CropManager.CropState.Grown)
        {
            if (normalStatusGroup) normalStatusGroup.SetActive(false);
            if (statusMessageText)
            {
                statusMessageText.gameObject.SetActive(true);
                statusMessageText.text = "Fully Grown\n(Press Interact to Harvest)";
            }
        }
        else // Dead
        {
            if (normalStatusGroup) normalStatusGroup.SetActive(false);
            if (statusMessageText)
            {
                statusMessageText.gameObject.SetActive(true);
                statusMessageText.text = "Dead\n(Press Interact to Clear)";
            }
        }
    }

    // -------- FarmPlot --------
    private void UpdateFarmPlotUI(FarmPlot plot)
    {
        if (normalStatusGroup) normalStatusGroup.SetActive(true);
        if (statusMessageText) statusMessageText.gameObject.SetActive(false);

        float tilled = Mathf.Clamp01(plot.TilledPercentNormalized);
        if (tilledSlider) tilledSlider.value = tilled;

        if (modeLabelText) modeLabelText.text = "Tilled";
    }

    // -------- Bed --------
    private void UpdateBedUI(Bed bed)
    {
        if (statusMessageText)
            statusMessageText.text = bed.GetInteractionText();
    }

    private void UpdateGenericNPCUI(InteractableNPC npc)
    {
        if (statusMessageText)
            statusMessageText.text = npc.GetInteractionText();
    }
}
