using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LEGACY: Old UI is completely disabled. 
/// - No input, no raycast, no per-frame work.
/// - On Awake: hides all known legacy UI roots (and optionally destroys them).
/// Keep attached only if you want safe cleanup without breaking references.
/// </summary>
[DisallowMultipleComponent]
public class InteractionUIController : MonoBehaviour
{
    [Header("Legacy UI Roots (optional)")]
    [SerializeField] private GameObject statusWindowGroup;   // 전체 패널(예: Canvas root)
    [SerializeField] private GameObject normalStatusGroup;   // 상세 바/텍스트 묶음
    [SerializeField] private TextMeshProUGUI statusMessageText;

    [Header("Legacy Detailed Widgets (optional)")]
    [SerializeField] private Slider waterSlider;
    [SerializeField] private TextMeshProUGUI waterValueText;
    [SerializeField] private Slider growthSlider;
    [SerializeField] private TextMeshProUGUI growthPercentText;
    [SerializeField] private TextMeshProUGUI growthTimeText;
    [SerializeField] private RectTransform lowZoneDanger;
    [SerializeField] private TextMeshProUGUI waterAmountText;
    [SerializeField] private RectTransform optimalZoneMarker;
    [SerializeField] private RectTransform currentValueMarker;
    [SerializeField] private RectTransform perfectZoneBand;

    [Header("Disable Options")]
    [Tooltip("If true, destroys the legacy UI GameObject(s) at runtime. If false, just SetActive(false).")]
    [SerializeField] private bool destroyLegacyObjects = false;

    private CanvasGroup _statusCanvasGroup;

    private void Awake()
    {
        // 1) Hide/deactivate the main UI root(s)
        if (statusWindowGroup != null)
        {
            _statusCanvasGroup = statusWindowGroup.GetComponent<CanvasGroup>();
            if (_statusCanvasGroup == null) _statusCanvasGroup = statusWindowGroup.AddComponent<CanvasGroup>();

            _statusCanvasGroup.alpha = 0f;
            _statusCanvasGroup.interactable = false;
            _statusCanvasGroup.blocksRaycasts = false;

            if (destroyLegacyObjects)
            {
                Destroy(statusWindowGroup);
            }
            else
            {
                statusWindowGroup.SetActive(false);
            }
        }

        if (normalStatusGroup != null)
        {
            if (destroyLegacyObjects) Destroy(normalStatusGroup);
            else normalStatusGroup.SetActive(false);
        }

        // 2) Optionally hide any loose widgets if they exist in scene as separate objects
        SafeHide(statusMessageText);
        SafeHide(waterSlider);
        SafeHide(waterValueText);
        SafeHide(growthSlider);
        SafeHide(growthPercentText);
        SafeHide(growthTimeText);
        SafeHide(lowZoneDanger);
        SafeHide(waterAmountText);
        SafeHide(optimalZoneMarker);
        SafeHide(currentValueMarker);
        SafeHide(perfectZoneBand);

        // 3) Disable this component to avoid any Update or future work
        enabled = false;
    }

    // --- Helpers ---
    private static void SafeHide(Behaviour b)
    {
        if (b == null) return;
        var go = b.gameObject;
        go.SetActive(false);
    }
    private static void SafeHide(RectTransform rt)
    {
        if (rt == null) return;
        rt.gameObject.SetActive(false);
    }
    private static void SafeHide(TextMeshProUGUI t)
    {
        if (t == null) return;
        t.gameObject.SetActive(false);
    }
    private static void SafeHide(Slider s)
    {
        if (s == null) return;
        s.gameObject.SetActive(false);
    }
}
