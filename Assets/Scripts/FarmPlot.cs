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
    [SerializeField] private float decayPerSec = 0.05f; // 초당 감소율
    [SerializeField] private bool lockAtFull = true;    // 100%면 감소 중지

    // 외부에서 읽기용
    public float TilledPercentNormalized => tilled01;
    public bool  IsFullyTilled           => tilled01 >= 1f - 1e-4f;

    // 이 프레임에 갈렸는지 표시 (감소 방지)
    private bool _tilledThisFrame = false;
    private Material _mat;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer != null)
        {
            // 인스턴스 머티리얼(다른 오브젝트와 공유 안 함)
            _mat = targetRenderer.material;
            ApplyColor();
        }
    }

    private void Update()
    {
        // 갈린 프레임이 아니고, 만충이 아니면 자연 감소
        if (!_tilledThisFrame && !(lockAtFull && IsFullyTilled))
        {
            tilled01 = Mathf.Max(0f, tilled01 - decayPerSec * Time.deltaTime);

            // 0%면 사라짐
            if (tilled01 <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            ApplyColor();
        }

        // 플래그 리셋
        _tilledThisFrame = false;
    }

    public void AddTill(float amount01) // amount01은 0~1 스케일
    {
        float before = tilled01;
        tilled01 = Mathf.Clamp01(tilled01 + amount01);
        _tilledThisFrame = true;

        if (!Mathf.Approximately(before, tilled01))
            ApplyColor();
    }

    private void ApplyColor()
    {
        if (_mat != null)
        {
            _mat.color = Color.Lerp(UntilledColor, TilledColor, tilled01);
        }
    }
}
