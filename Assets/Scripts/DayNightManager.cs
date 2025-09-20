using UnityEngine;
using TMPro;

public class DayNightManager : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("현실 기준 한 ‘게임 하루’가 몇 분 걸리는지")]
    [SerializeField] private float dayLengthMinutes = 10f;  // ex) 10분 = 현실 10분에 게임 24시간
    [Tooltip("게임 시작 시각 (0~24)")]
    [Range(0f, 24f)]
    [SerializeField] private float startHour = 8f;
    [Tooltip("일출 시각 (0~24)")]
    [Range(0f, 24f)]
    [SerializeField] private float sunriseHour = 6f;
    [Tooltip("일몰 시각 (0~24)")]
    [Range(0f, 24f)]
    [SerializeField] private float sunsetHour = 18f;
    [Tooltip("Time.timeScale의 영향을 받지 않게 현실 초로 진행할지 여부")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Light Settings")]
    [SerializeField] private Light directionalLight;
    [Tooltip("하루 진행도(0~1)에 따른 강도 곡선 (비우면 자동 계산)")]
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.Linear(0, 0, 1, 0);
    [Tooltip("하루 진행도(0~1)에 따른 색상 (비우면 고정색)")]
    [SerializeField] private Gradient colorOverDay;
    [Tooltip("정오 기준 광량 스케일")]
    [SerializeField] private float maxIntensity = 1.2f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI dayLabel;   // "Day 1"
    [SerializeField] private TextMeshProUGUI timeLabel;  // "HH:MM"

    // 내부 상태
    private float gameMinutesInDay;   // 0..1440
    private int dayCount = 1;

    // 편의 상수
    private const float MIN_PER_DAY = 1440f;

    void Start()
    {
        startHour = Mathf.Repeat(startHour, 24f);
        gameMinutesInDay = startHour * 60f;

        // intensityCurve 기본값이 Linear(0->0,1->0)이면, 간이 일출/일몰 기반으로 보정
        if (intensityCurve != null && intensityCurve.keys.Length <= 2)
        {
            // 간단한 "밤(0) → 낮(1) → 밤(0)" 형태를 구성
            float sr = sunriseHour / 24f; // 0~1
            float ss = sunsetHour / 24f;  // 0~1
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, 0f, 0, 0));
            curve.AddKey(new Keyframe(sr, 1f, 0, 0));
            curve.AddKey(new Keyframe(ss, 1f, 0, 0));
            curve.AddKey(new Keyframe(1f, 0f, 0, 0));
            intensityCurve = curve;
        }
    }

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // 현실 1초당 진행되는 '게임 분' = 1440 / (하루길이(초))
        float gameMinutesPerRealSecond = MIN_PER_DAY / (Mathf.Max(0.01f, dayLengthMinutes) * 60f);
        gameMinutesInDay += gameMinutesPerRealSecond * dt;

        // 넘치면 다음 날로
        if (gameMinutesInDay >= MIN_PER_DAY)
        {
            int wrappedDays = Mathf.FloorToInt(gameMinutesInDay / MIN_PER_DAY);
            dayCount += wrappedDays;
            gameMinutesInDay = Mathf.Repeat(gameMinutesInDay, MIN_PER_DAY);
        }

        UpdateLight();
        UpdateUI();
    }

    private void UpdateLight()
    {
        if (directionalLight == null) return;

        float t01 = gameMinutesInDay / MIN_PER_DAY; // 0..1 (하루 진행도)
        // 태양 고도 회전: 0시 = -90°, 6시=0°, 12시=90°, 18시=180°
        float sunAngle = t01 * 360f - 90f;
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // 강도
        float intensity = maxIntensity;
        if (intensityCurve != null)
            intensity = maxIntensity * Mathf.Clamp01(intensityCurve.Evaluate(t01));
        directionalLight.intensity = intensity;

        // 색상
        if (colorOverDay != null && colorOverDay.colorKeys != null && colorOverDay.colorKeys.Length > 0)
        {
            directionalLight.color = colorOverDay.Evaluate(t01);
        }
    }

    private void UpdateUI()
    {
        if (dayLabel != null)
            dayLabel.text = $"Day {dayCount}";

        if (timeLabel != null)
        {
            int hours = Mathf.FloorToInt(gameMinutesInDay / 60f) % 24;
            int minutes = Mathf.FloorToInt(gameMinutesInDay % 60f);
            timeLabel.text = $"{hours:00}:{minutes:00}";
        }
    }

    // ===== Public API =====

    /// <summary>하루 길이를 ‘현실 분’ 기준으로 설정</summary>
    public void SetDayLengthMinutes(float minutes)
    {
        dayLengthMinutes = Mathf.Max(0.01f, minutes);
    }

    /// <summary>지정 ‘게임 분’만큼 즉시 스킵 (수면 등)</summary>
    public void SkipGameMinutes(float minutes)
    {
        minutes = Mathf.Max(0f, minutes);
        gameMinutesInDay += minutes;

        if (gameMinutesInDay >= MIN_PER_DAY)
        {
            int addDays = Mathf.FloorToInt(gameMinutesInDay / MIN_PER_DAY);
            dayCount += addDays;
            gameMinutesInDay = Mathf.Repeat(gameMinutesInDay, MIN_PER_DAY);
        }

        UpdateLight();
        UpdateUI();
    }

    /// <summary>지정 ‘게임 시간(시)’만큼 즉시 스킵</summary>
    public void SleepHours(float hours)
    {
        SkipGameMinutes(hours * 60f);
    }

    /// <summary>현재 (0~24) 시간 반환</summary>
    public float GetCurrentHour()
    {
        return (gameMinutesInDay / 60f) % 24f;
    }

    /// <summary>일출/일몰 시각(0~24) 설정</summary>
    public void SetSunriseSunset(float sunrise, float sunset)
    {
        sunriseHour = Mathf.Repeat(sunrise, 24f);
        sunsetHour = Mathf.Repeat(sunset, 24f);
    }
}
