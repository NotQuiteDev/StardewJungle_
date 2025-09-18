using UnityEngine;
using UnityEngine.UI; // Slider를 사용하기 위해 필요
using TMPro; // TextMeshPro를 사용하기 위해 필요

public class InteractionUIController : MonoBehaviour
{
    [Header("레이캐스트 설정")]
    [SerializeField] private float raycastDistance = 5f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI 요소 연결")]
    [SerializeField] private GameObject statusWindowGroup;
    [SerializeField] private Slider waterSlider;
    [SerializeField] private TextMeshProUGUI waterValueText;
    [SerializeField] private Slider growthSlider;
    [SerializeField] private TextMeshProUGUI growthPercentText;
    [SerializeField] private TextMeshProUGUI growthTimeText;

    private Transform cameraTransform;

    private void Awake()
    {
        cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        // 카메라 중앙에서 레이를 쏜다
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        
        // interactableLayer에 대해서만 레이를 쏜다
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactableLayer))
        {
            // 레이에 맞은 오브젝트에서 CropManager 컴포넌트를 찾아본다
            if (hit.collider.TryGetComponent(out CropManager crop))
            {
                // 찾았다면 UI를 켜고, 정보를 업데이트한다
                statusWindowGroup.SetActive(true);
                UpdateStatusUI(crop);
            }
            else
            {
                // CropManager가 없다면 UI를 끈다
                statusWindowGroup.SetActive(false);
            }
        }
        else
        {
            // 레이에 아무것도 맞지 않았다면 UI를 끈다
            statusWindowGroup.SetActive(false);
        }
    }

    /// <summary>
    /// CropManager의 정보를 받아와 UI를 업데이트하는 함수
    /// </summary>
    private void UpdateStatusUI(CropManager crop)
    {
        // --- 수분 UI 업데이트 ---
        float waterPercent = crop.CurrentWaterAmount / crop.MaxWaterAmount;
        waterSlider.value = waterPercent;
        waterValueText.text = $"Water: {waterPercent * 100f:F0}%";

        // --- 성장 UI 업데이트 ---
        float growthPercent = crop.GrowthTimer / crop.GrowthDuration;
        growthSlider.value = growthPercent;
        growthPercentText.text = $"Grown: {growthPercent * 100f:F0}%";
        
        float timeRemaining = crop.GrowthDuration - crop.GrowthTimer;
        // 0초 미만으로 내려가지 않도록 보정
        timeRemaining = Mathf.Max(0, timeRemaining); 
        
        // 분:초 형태로 변환
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        growthTimeText.text = $"{minutes:00}:{seconds:00}";
    }
}