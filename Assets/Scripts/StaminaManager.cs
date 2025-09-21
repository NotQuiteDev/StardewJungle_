using UnityEngine;
using UnityEngine.UI; // UI.Slider 사용을 위해 추가

public class StaminaManager : MonoBehaviour
{
    public static StaminaManager Instance { get; private set; }

    [Header("스태미나 설정")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenPerSecond = 2f; // 초당 회복량
    [SerializeField] private Slider staminaBar; // UI 슬라이더 연결

    private float currentStamina;

    public float MaxStamina => maxStamina;
    public float CurrentStamina => currentStamina;

    private void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    }

    private void Start()
    {
        currentStamina = maxStamina;
        UpdateStaminaBar();
    }

    private void Update()
    {
        // 자연 회복 로직
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenPerSecond * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
            UpdateStaminaBar();
        }
    }

    public bool UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            UpdateStaminaBar();
            return true; // 사용 성공
        }
        // 여기에 "스태미나가 부족합니다!" 같은 UI 피드백을 추가할 수 있습니다.
        return false; // 사용 실패
    }

    public void RestoreStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        UpdateStaminaBar();
    }
    
    public void RestoreStaminaToFull()
    {
        currentStamina = maxStamina;
        UpdateStaminaBar();
    }

    private void UpdateStaminaBar()
    {
        if (staminaBar != null)
        {
            staminaBar.value = currentStamina / maxStamina;
        }
    }
}