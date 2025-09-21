using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class SleepManager : MonoBehaviour
{
    [Header("시간 설정")]
    [SerializeField] private float sleepTimeScale = 30f;
    [SerializeField] private float minSleepRealtime = 0.3f;

    // ## 추가: 잠자는 동안의 스태미나 회복 설정 ##
    [Header("스태미나 회복 설정")]
    [Tooltip("잠자는 동안 초당 회복될 스태미나 양 (게임 시간 기준)")]
    [SerializeField] private float staminaRegenPerSecondWhileSleeping = 12.5f;

    [Header("연결")]
    [SerializeField] private DayNightManager dayNight;
    [SerializeField] private PlayerMovement playerMovement;

    private bool isSleeping = false;
    private float sleepStartRealtime;
    private bool prevUseUnscaled = true;

    public bool IsSleeping => isSleeping;

    private void Awake()
    {
        if (dayNight == null) dayNight = FindObjectOfType<DayNightManager>();
        if (playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
    }

    private void Update()
    {
        if (!isSleeping) return;

        // ## 추가: 잠자는 동안 매 프레임 스태미나를 회복시킵니다. ##
        // Time.timeScale의 영향을 받지 않도록 unscaledDeltaTime을 사용합니다.
        float amountToRestore = staminaRegenPerSecondWhileSleeping * Time.deltaTime;
        StaminaManager.Instance.RestoreStamina(amountToRestore);

        // 너무 빨리 깨는 것을 방지
        if (Time.realtimeSinceStartup - sleepStartRealtime < minSleepRealtime) return;

        if (AnyInputPressedThisFrame())
            Wake();
    }

    public void StartSleep()
    {
        if (isSleeping) return;
        isSleeping = true;
        sleepStartRealtime = Time.realtimeSinceStartup;

        playerMovement?.LockMovement();

        if (dayNight != null)
        {
            prevUseUnscaled = GetUseUnscaled(dayNight);
            SetUseUnscaled(dayNight, false);
        }
        
        Time.timeScale = Mathf.Max(0.1f, sleepTimeScale);
    }

    public void Wake()
    {
        if (!isSleeping) return;

        Time.timeScale = 1f;
        
        playerMovement?.UnlockMovement();

        if (dayNight != null)
            SetUseUnscaled(dayNight, prevUseUnscaled);
        
        isSleeping = false;
    }

    // (이하 ForceWake, AnyInputPressedThisFrame 등 나머지 함수는 기존과 동일)
    private void OnDisable(){ if (isSleeping) ForceWake(); }
    private void ForceWake()
    {
        Time.timeScale = 1f;
        playerMovement?.UnlockMovement();
        if (dayNight != null) SetUseUnscaled(dayNight, prevUseUnscaled);
        isSleeping = false;
    }
    private static bool AnyInputPressedThisFrame() 
    { 
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame ||
                Mouse.current.middleButton.wasPressedThisFrame)
                return true;
        }
        return false; 
    }
    private static bool GetUseUnscaled(DayNightManager m) 
    {
        var f = typeof(DayNightManager).GetField("useUnscaledTime",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f != null ? (bool)f.GetValue(m) : true;
    }
    private static void SetUseUnscaled(DayNightManager m, bool v) 
    {
        var f = typeof(DayNightManager).GetField("useUnscaledTime",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (f != null) f.SetValue(m, v);
    }
}