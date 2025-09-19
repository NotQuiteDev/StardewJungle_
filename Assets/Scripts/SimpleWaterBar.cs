using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 간단 머리위 바: 현재 수분(Fill) + 그린존 밴드 + 현재 위치 핸들.
/// - 플레이어 근접 시만 표시
/// - 작물이 Growing이 아니면 자동 숨김(또는 파괴)
/// </summary>
[DisallowMultipleComponent]
public class SimpleWaterBar : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject worldBarPrefab;       // WorldBar_Simple

    [Header("Attach / Position")]
    public Transform head;                  // 비우면 this.transform
    public float yOffset = 1.6f;            // 머리 위 오프셋

    [Header("Visibility")]
    public float showWithinDistance = 10f;  // 0이면 항상 보임
    public string playerTag = "Player";     // 플레이어 찾기용
    public bool billboard = true;
    public bool hideWhenBehindCamera = true;

    [Header("State-based Visibility")]
    [Tooltip("Growing이 아닐 때 바를 숨김")]
    public bool hideWhenNotGrowing = true;
    [Tooltip("Growing이 아닐 때 컴포넌트를 파괴(오브젝트도 함께 삭제됨)")]
    public bool destroyOnNotGrowing = false;

    private Transform _bar;
    private RectTransform _barArea;
    private Image _fill;
    private RectTransform _perfectZone;
    private RectTransform _currentHandle;

    private CropManager _crop;
    private Camera _cam;
    private Transform _player;

    private void Awake()
    {
        _crop = GetComponent<CropManager>();
        if (!_crop) { enabled = false; return; }

        if (!head) head = transform;
        _cam = Camera.main ? Camera.main : FindFirstObjectByType<Camera>();

        var playerGo = GameObject.FindGameObjectWithTag(playerTag);
        _player = playerGo ? playerGo.transform : null;
    }

    private void Start()
    {
        if (!worldBarPrefab) { enabled = false; return; }

        // 프리팹 인스턴스 (부모는 작물)
        var go = Instantiate(worldBarPrefab, transform.position, Quaternion.identity, transform);
        _bar = go.transform;

        // 자식 참조
        _barArea       = go.transform.Find("BarArea")?.GetComponent<RectTransform>();
        _fill          = go.transform.Find("BarArea/Fill")?.GetComponent<Image>();
        _perfectZone   = go.transform.Find("BarArea/PerfectZone")?.GetComponent<RectTransform>();
        _currentHandle = go.transform.Find("BarArea/CurrentHandle")?.GetComponent<RectTransform>();

        // 안전 세팅
        ForceLeftAnchors(_barArea);
        if (_fill)
        {
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill.fillAmount = 0f;
            _fill.raycastTarget = false;
        }
        ForceLeftAnchors(_perfectZone);
        ForceLeftAnchors(_currentHandle);
        DisableRaycasts(go);
    }

    private void LateUpdate()
    {
        if (!_bar || !_barArea || !_crop) return;

        // ★ Growing 상태가 아니면 숨기거나 제거
        if (hideWhenNotGrowing && _crop.State != CropManager.CropState.Growing)
        {
            if (destroyOnNotGrowing)
            {
                Destroy(_bar.gameObject);
                Destroy(this);
                return;
            }
            else
            {
                if (_bar.gameObject.activeSelf) _bar.gameObject.SetActive(false);
                return;
            }
        }

        // 위치 계산
        Vector3 pos = (head ? head.position : transform.position) + Vector3.up * yOffset;

        // 가시성(거리 + 카메라 뒤)
        bool visible = true;
        if (_player && showWithinDistance > 0f)
        {
            float d = Vector3.Distance(_player.position, transform.position);
            visible = d <= showWithinDistance;
        }
        if (visible && hideWhenBehindCamera && _cam)
        {
            Vector3 dir = pos - _cam.transform.position;
            if (Vector3.Dot(_cam.transform.forward, dir) < 0f) visible = false;
        }

        if (!visible) { if (_bar.gameObject.activeSelf) _bar.gameObject.SetActive(false); return; }
        if (!_bar.gameObject.activeSelf) _bar.gameObject.SetActive(true);

        _bar.position = pos;
        if (billboard && _cam) _bar.rotation = Quaternion.LookRotation(_bar.position - _cam.transform.position, Vector3.up);

        // 값 계산 (정규화 0..1)
        float min = _crop.MinWaterAmount;
        float max = _crop.MaxWaterAmount;
        float cur = _crop.CurrentWaterAmount;
        float opt = _crop.OptimalWaterAmount;

        float pCur = Mathf.Clamp01(Mathf.InverseLerp(min, max, cur));
        float pOpt = Mathf.Clamp01(Mathf.InverseLerp(min, max, opt));

        // CropManager에 GreenZoneWidth01 프로퍼티가 있으면 사용, 없으면 0.2
        float greenWidth01 = GetGreenZoneWidth01OrDefault();
        float half = Mathf.Clamp01(greenWidth01) * 0.5f;
        float pL = Mathf.Clamp01(pOpt - half);
        float pR = Mathf.Clamp01(pOpt + half);

        float w = _barArea.rect.width;

        // Fill
        if (_fill) _fill.fillAmount = pCur;

        // PerfectZone 밴드
        if (_perfectZone)
        {
            _perfectZone.anchoredPosition = new Vector2(w * pL, _perfectZone.anchoredPosition.y);
            var sd = _perfectZone.sizeDelta;
            sd.x = Mathf.Max(0f, (pR - pL) * w);
            _perfectZone.sizeDelta = sd;
        }

        // 현재 위치 핸들
        if (_currentHandle)
        {
            var ap = _currentHandle.anchoredPosition;
            ap.x = w * pCur;
            _currentHandle.anchoredPosition = ap;
        }
    }

    private float GetGreenZoneWidth01OrDefault()
    {
        var prop = typeof(CropManager).GetProperty("GreenZoneWidth01",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (prop != null) {
            object v = prop.GetValue(_crop, null);
            if (v is float f) return Mathf.Clamp01(f);
        }
        return 0.20f; // fallback
    }

    private static void ForceLeftAnchors(RectTransform rt)
    {
        if (!rt) return;
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot     = new Vector2(0f, 0.5f);
    }

    private static void DisableRaycasts(GameObject root)
    {
        foreach (var img in root.GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;
    }
}
