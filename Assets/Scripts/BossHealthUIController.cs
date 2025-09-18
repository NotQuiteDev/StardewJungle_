// 파일 이름: BossHealthUIController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthUIController : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private GameObject uiContainer;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI bossNameText;

    private EnemyHealth bossHealth;

    void Awake()
    {
        if (uiContainer != null) uiContainer.SetActive(false);
    }
    
    private void OnEnable()
    {
        // PlayerCheckpoint의 리스폰 이벤트를 구독합니다.
        PlayerCheckpoint.OnPlayerRespawn += Hide;
    }

    private void OnDisable()
    {
        // 구독을 해지하여 메모리 누수를 방지합니다.
        PlayerCheckpoint.OnPlayerRespawn -= Hide;
    }

    public void Setup(EnemyHealth targetBossHealth)
    {
        this.bossHealth = targetBossHealth;
        if (this.bossHealth != null)
        {
            healthSlider.maxValue = bossHealth.maxHealth;
            healthSlider.value = bossHealth.currentHealth;
            // 체력 변경 이벤트를 구독합니다.
            bossHealth.OnHealthChanged += UpdateHealth;
        }
        uiContainer.SetActive(true);
    }
    
    private void UpdateHealth(float currentHealth, float maxHealth)
    {
        healthSlider.value = currentHealth;
    }

    public void Hide()
    {
        // UI를 숨길 때, 보스의 체력 이벤트 구독도 함께 해지합니다.
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= UpdateHealth;
            bossHealth = null;
        }
        if (uiContainer != null)
        {
            uiContainer.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // 만약을 위해 OnDestroy에서도 모든 이벤트 구독을 해지합니다.
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= UpdateHealth;
        }
        PlayerCheckpoint.OnPlayerRespawn -= Hide;
    }
}