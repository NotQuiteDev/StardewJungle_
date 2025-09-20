using UnityEngine;

[DisallowMultipleComponent]
public class FarmPlot : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Renderer targetRenderer; // 비우면 자동으로 MeshRenderer 찾음
    [SerializeField] private Color UntilledColor = new Color(0.85f, 0.80f, 0.70f);
    [SerializeField] private Color TilledColor   = new Color(0.35f, 0.22f, 0.10f);

    [Header("Logic")]
    [SerializeField] [Range(0f, 1f)] private float tilled01 = 0f; // 0~1
    [SerializeField] private float decayPerSec = 0.05f;           // 초당 감소율
    [SerializeField] private bool  lockAtFull = true;             // 100%면 감소 중지(보류용)

    // 외부 읽기
    public float TilledPercentNormalized => tilled01;
    public bool  IsFullyTilled          => tilled01 >= 1f - 1e-4f;
    
    // ## 추가된 부분 ##
    public bool IsReadyForPlanting => tilled01 >= 0.8f;

    // 프레임 플래그
    private bool _tilledThisFrame = false;
    private Material _mat;

    [SerializeField] private Transform plantAnchor; // 선택

    [Header("Full Hold & Consume")]
    [SerializeField] private float fullHoldSeconds = 30f; // 완전 경작 유지 시간
    private float _timeBecameFull = -1f;                  // 1.0 달성 시각

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer != null)
        {
            _mat = targetRenderer.material; // 인스턴스 머티리얼
            ApplyColor();
        }
    }

    private void Update()
    {
        bool occupied = HasAnyCrop();

        // 1) (작물 없을 때만) 1.0 유지 타이머 처리 → 0.99로 강등
        if (!occupied && tilled01 >= 1f - 1e-6f && fullHoldSeconds > 0f && _timeBecameFull > 0f)
        {
            float held = Time.time - _timeBecameFull;
            if (held >= fullHoldSeconds)
            {
                tilled01 = 0.99f;
                _timeBecameFull = -1f;
                ApplyColor();
            }
        }

        // 2) 자연 감쇠 (작물이 있으면 절대 감쇠하지 않음)
        bool atFullAndHolding = !occupied &&
                                (tilled01 >= 1f - 1e-6f) &&
                                (_timeBecameFull > 0f) &&
                                (Time.time - _timeBecameFull < fullHoldSeconds);

        if (!_tilledThisFrame && !occupied && !atFullAndHolding && !(lockAtFull && IsFullyTilled))
        {
            tilled01 = Mathf.Max(0f, tilled01 - decayPerSec * Time.deltaTime);

            if (tilled01 <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            ApplyColor();
        }

        _tilledThisFrame = false;
    }

    public void AddTill(float amount01)
    {
        float before = tilled01;
        tilled01 = Mathf.Clamp01(tilled01 + amount01);
        _tilledThisFrame = true;

        if (tilled01 >= 1f - 1e-6f && before < 1f - 1e-6f)
            _timeBecameFull = Time.time;

        if (!Mathf.Approximately(before, tilled01))
            ApplyColor();
    }

    private void ApplyColor()
    {
        if (_mat != null)
            _mat.color = Color.Lerp(UntilledColor, TilledColor, tilled01);
    }

    public bool HasAnyCrop()
    {
        return GetComponentInChildren<CropManager>() != null;
    }

    public Vector3 GetPlantSpawnPoint(float yOffsetFallback)
    {
        if (plantAnchor != null) return plantAnchor.position;
        return new Vector3(transform.position.x, transform.position.y + yOffsetFallback, transform.position.z);
    }
    
    public void OnHarvestedReduceToHalf()
    {
        tilled01 = Mathf.Min(tilled01, 0.5f);
        _timeBecameFull = -1f;
        _tilledThisFrame = true;
        ApplyColor();
    }
}