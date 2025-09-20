using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class SleepManager : MonoBehaviour
{
    [Header("Fast-Forward")]
    [SerializeField] private float sleepTimeScale = 30f;
    [SerializeField] private float minSleepRealtime = 0.3f;

    [Header("Optional")]
    [SerializeField] private DayNightManager dayNight;

    [Header("Required")]
    [SerializeField] private PlayerMovement playerMovement; // 플레이어 무브먼트 스크립트 참조

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

        if (AnyInputPressedThisFrame())
            Wake();
    }

    public void StartSleep()
    {
        if (isSleeping) return;
        isSleeping = true;
        sleepStartRealtime = Time.realtimeSinceStartup;

        if (playerMovement != null)
            playerMovement.LockMovement();

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
        if (Time.realtimeSinceStartup - sleepStartRealtime < minSleepRealtime) return;

        Time.timeScale = 1f;
        
        if (playerMovement != null)
            playerMovement.UnlockMovement();

        if (dayNight != null)
            SetUseUnscaled(dayNight, prevUseUnscaled);

        isSleeping = false;
    }

    private void OnDisable()
    {
        if (isSleeping) ForceWake();
    }

    private void ForceWake()
    {
        Time.timeScale = 1f;
        
        if (playerMovement != null)
            playerMovement.UnlockMovement();

        if (dayNight != null)
            SetUseUnscaled(dayNight, prevUseUnscaled);

        isSleeping = false;
    }

    private static bool AnyInputPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame ||
                Mouse.current.middleButton.wasPressedThisFrame ||
                Mouse.current.forwardButton.wasPressedThisFrame ||
                Mouse.current.backButton.wasPressedThisFrame)
                return true;
        }

        foreach (var device in InputSystem.devices)
        {
            foreach(var control in device.allControls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame)
                    return true;
            }
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