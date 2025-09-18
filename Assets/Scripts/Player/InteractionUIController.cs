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

    private void Awake()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (raycastCamera == null) return;

        Ray ray = new Ray(raycastCamera.transform.position, raycastCamera.transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactableLayer))
        {
            // ▼▼▼ 핵심 수정 ▼▼▼
            // hit.collider에서 직접 찾는 대신, 부모 오브젝트까지 모두 검색해서 CropManager를 찾습니다.
            CropManager crop = hit.collider.GetComponentInParent<CropManager>();

            // 부모를 포함해서 CropManager를 찾았다면
            if (crop != null)
            {
                statusWindowGroup.SetActive(true);
                UpdateStatusUI(crop);
                return;
            }
            // ▲▲▲ 핵심 수정 ▲▲▲
        }

        statusWindowGroup.SetActive(false);
    }

    private void UpdateStatusUI(CropManager crop)
    {
        // 수분 UI 업데이트
        float waterPercent = crop.CurrentWaterAmount / crop.MaxWaterAmount;
        waterSlider.value = waterPercent;
        waterValueText.text = $"Water: {waterPercent * 100f:F0}%";

        // 성장 UI 업데이트
        float growthPercent = crop.GrowthTimer / crop.GrowthDuration;
        growthSlider.value = growthPercent;
        growthPercentText.text = $"Grown: {growthPercent * 100f:F0}%";
        
        float timeRemaining = crop.GrowthDuration - crop.GrowthTimer;
        timeRemaining = Mathf.Max(0, timeRemaining);
        
        // 분:초 형태로 변환
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        growthTimeText.text = $"{minutes:00}:{seconds:00}";
    }
}