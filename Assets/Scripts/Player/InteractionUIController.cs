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
    [SerializeField] private GameObject normalStatusGroup;
    [SerializeField] private TextMeshProUGUI statusMessageText;

    [Header("UI 슬라이더")]
    [SerializeField] private Slider growthSlider;      // Crop: 성장 슬라이더
    [SerializeField] private Slider tilledSlider;      // ## FarmPlot과 MineableStone이 공유해서 사용할 슬라이더 ##
    // [SerializeField] private Slider stoneHealthSlider; // ## 이 줄은 삭제합니다! ##

    [Header("라벨 표시")]
    [SerializeField] private TextMeshProUGUI modeLabelText;

    private CanvasGroup statusCanvasGroup;

    private enum TargetMode { None, Crop, FarmPlot, Bed, GenericNPC, MineableStone }
    private TargetMode _mode = TargetMode.None;

    private void Awake()
    {
        if (raycastCamera == null) raycastCamera = Camera.main;
        if (statusWindowGroup != null)
        {
            statusCanvasGroup = statusWindowGroup.GetComponent<CanvasGroup>();
            if (statusCanvasGroup == null)
                statusCanvasGroup = statusWindowGroup.AddComponent<CanvasGroup>();
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
            // ## 우선순위 1: Crop (가장 구체적인 대상) ##
            CropManager crop = hit.collider.GetComponentInParent<CropManager>();
            if (crop != null)
            {
                SetMode(TargetMode.Crop);
                statusCanvasGroup.alpha = 1f;
                UpdateCropUI(crop);
                return;
            }

            // ## 우선순위 2: MineableStone ##
            MineableStone stone = hit.collider.GetComponentInParent<MineableStone>();
            if (stone != null)
            {
                SetMode(TargetMode.MineableStone);
                statusCanvasGroup.alpha = 1f;
                UpdateMineableStoneUI(stone);
                return;
            }

            // ## 우선순위 3: FarmPlot (Crop이 없을 때만 감지됨) ##
            FarmPlot plot = hit.collider.GetComponentInParent<FarmPlot>();
            if (plot != null)
            {
                SetMode(TargetMode.FarmPlot);
                statusCanvasGroup.alpha = 1f;
                UpdateFarmPlotUI(plot);
                return;
            }

            // ## 나머지 상호작용 오브젝트들 ##
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
                UpdateGenericNPCUI(npc);
                return;
            }
        }

        // 감지된 대상이 아무것도 없음
        SetMode(TargetMode.None);
        statusCanvasGroup.alpha = 0f;
    }

    private void SetMode(TargetMode newMode)
    {
        if (_mode == newMode) return;
        _mode = newMode;

        // 모드가 바뀔 때마다 모든 슬라이더를 끄고 시작하면 관리가 편함
        if (growthSlider) growthSlider.gameObject.SetActive(false);
        if (tilledSlider) tilledSlider.gameObject.SetActive(false);

        switch (_mode)
        {
            case TargetMode.MineableStone:
                if (normalStatusGroup) normalStatusGroup.SetActive(true);
                if (statusMessageText) statusMessageText.gameObject.SetActive(false);
                if (tilledSlider) tilledSlider.gameObject.SetActive(true); // ## 공유 슬라이더 켜기 ##
                if (modeLabelText) modeLabelText.gameObject.SetActive(true);
                break;

            case TargetMode.Crop:
                if (normalStatusGroup) normalStatusGroup.SetActive(true);
                if (statusMessageText) statusMessageText.gameObject.SetActive(false);
                if (growthSlider) growthSlider.gameObject.SetActive(true);
                if (modeLabelText) modeLabelText.gameObject.SetActive(true);
                break;

            case TargetMode.FarmPlot:
                if (normalStatusGroup) normalStatusGroup.SetActive(true);
                if (statusMessageText) statusMessageText.gameObject.SetActive(false);
                if (tilledSlider) tilledSlider.gameObject.SetActive(true);
                if (modeLabelText) modeLabelText.gameObject.SetActive(true);
                break;

            case TargetMode.Bed:
            case TargetMode.GenericNPC:
                if (normalStatusGroup) normalStatusGroup.SetActive(false);
                if (statusMessageText) statusMessageText.gameObject.SetActive(true);
                if (modeLabelText) modeLabelText.gameObject.SetActive(false);
                break;

            case TargetMode.None:
                break;
        }
    }

    private void UpdateMineableStoneUI(MineableStone stone)
    {
        float healthPercent = (stone.MaxHealth > 0) ? stone.CurrentHealth / stone.MaxHealth : 0;
        
        // ## stoneHealthSlider 대신 tilledSlider를 사용 ##
        if (tilledSlider) tilledSlider.value = healthPercent;

        if (modeLabelText) modeLabelText.text = "Mineable";
    }

    private void UpdateCropUI(CropManager crop)
    {
        // (이하 기존과 동일)
        if (crop.State == CropManager.CropState.Growing)
        {
            if (normalStatusGroup) normalStatusGroup.SetActive(true);
            if (statusMessageText) statusMessageText.gameObject.SetActive(false);

            float growthPercent = (crop.GrowthDuration > 0f) ? Mathf.Clamp01(crop.GrowthTimer / crop.GrowthDuration) : 0f;

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
