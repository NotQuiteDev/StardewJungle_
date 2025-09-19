using UnityEngine;
using System.Collections;

public class WateringCanRuntime : MonoBehaviour
{
    // 외부에서 주입되는 참조/설정
    private WateringCanData _data;
    private Transform _equip;
    private Transform _cam;

    // 파티클 인스턴스(재사용)
    private GameObject _psGO;
    private ParticleSystem _ps;

    // 루프/상태
    private Coroutine _loop;
    private bool _isWatering = false;
    private bool _loopRequested = false;

    // 물리 히트 & 크롭 버퍼(고정 크기, GC 없음)
    private const int MaxHits = 64;
    private static readonly Collider[] _hits = new Collider[MaxHits];
    private static readonly CropManager[] _crops = new CropManager[MaxHits];

    [Header("Visual")]
    [SerializeField] private bool forceHorizontalParticleDirection = true;

    // -----------------------
    // 공개 API
    // -----------------------
    public void StartWatering(WateringCanData data, Transform equip, Transform cam)
    {
        _data = data;
        _equip = equip;
        _cam = cam;

        EnsureParticleExists();

        if (_ps != null)
        {
            if (_equip != null)
            {
                _psGO.transform.position = _equip.position;
                _psGO.transform.rotation = _equip.rotation;
            }

            _ps.Clear(true);
            var emission = _ps.emission; emission.enabled = true;
            if (!_ps.isPlaying) _ps.Play(true);
        }

        _isWatering = true;
    }

    public void StopWatering()
    {
        if (!_isWatering) return;
        _isWatering = false;

        if (_ps != null)
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // -----------------------
    // 내부 루프
    // -----------------------
    private IEnumerator WaterLoop()
    {
        while (true)
        {
            // 노즐 추적
            if (_psGO != null && _equip != null)
            {
                _psGO.transform.position = _equip.position;
                _psGO.transform.rotation = _equip.rotation;
            }

            // 물 공급: 원통(캡슐) 영역 내 모든 CropManager에 물 주기
            if (_isWatering && _cam != null && _data != null)
            {
                // 시작점: 노즐 있으면 노즐, 없으면 카메라 위치
                Vector3 origin = (_equip != null) ? _equip.position : _cam.position;

                // ★ 수평(지표와 평행) 방향
                Vector3 dirH = HorizontalDir(_cam, _equip); // Y=0, 정규화

                // (옵션) 파티클도 수평 방향을 보게 만들기
                if (_psGO != null && forceHorizontalParticleDirection)
                {
                    _psGO.transform.rotation = Quaternion.LookRotation(dirH, Vector3.up);
                }

                // 캡슐 양 끝점 (알약 길이 = sprayLength, 수평 방향)
                Vector3 p0 = origin;
                Vector3 p1 = origin + dirH * _data.sprayLength;

                int layerMask = (_data.waterableLayer.value == 0)
                                ? Physics.DefaultRaycastLayers
                                : _data.waterableLayer.value;

                int hitCount = Physics.OverlapCapsuleNonAlloc(p0, p1, _data.sprayRadius, _hits, layerMask);

                // 유니크 CropManager 수집
                int uniqueCount = 0;
                for (int i = 0; i < hitCount && uniqueCount < MaxHits; i++)
                {
                    var col = _hits[i];
                    if (!col) continue;
                    var crop = col.GetComponentInParent<CropManager>();
                    if (crop == null) continue;

                    bool seen = false;
                    for (int k = 0; k < uniqueCount; k++)
                    {
                        if (_crops[k] == crop) { seen = true; break; }
                    }
                    if (!seen) _crops[uniqueCount++] = crop;
                }

                float dtAmount = _data.waterPerSecond * Time.deltaTime;

                if (uniqueCount > 0)
                {
                    float each = _data.distributeWaterEvenly ? (dtAmount / uniqueCount) : dtAmount;
                    for (int i = 0; i < uniqueCount; i++)
                    {
                        var crop = _crops[i];
                        if (crop != null) crop.WaterCrop(each);
                        _crops[i] = null;
                    }
                }
                else
                {
                    // 백업: 싱글 레이도 수평 방향으로
                    Ray ray = new Ray(origin, dirH);
                    if (Physics.Raycast(ray, out RaycastHit hit, _data.raycastDistance, layerMask))
                    {
                        var crop = hit.collider.GetComponentInParent<CropManager>();
                        if (crop != null) crop.WaterCrop(dtAmount);
                    }
                }
            }

            yield return null;
        }
    }

    private static Vector3 HorizontalDir(Transform primary, Transform fallback)
    {
        Vector3 d = primary ? primary.forward : (fallback ? fallback.forward : Vector3.forward);
        d.y = 0f;
        if (d.sqrMagnitude < 1e-6f)
        {
            d = fallback ? new Vector3(fallback.forward.x, 0f, fallback.forward.z) : Vector3.forward;
        }
        return d.normalized;
    }

    private void OnEnable()
    {
        if (_loop == null) _loop = StartCoroutine(WaterLoop());
    }

    private void OnDisable()
    {
        _isWatering = false;

        if (_ps != null)
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    private void OnDestroy()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
        _loopRequested = false;
        _isWatering = false;

        if (_ps != null)
        {
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _ps = null;
        }
        if (_psGO != null)
        {
            Destroy(_psGO);
            _psGO = null;
        }
    }

    // -----------------------
    // 유틸
    // -----------------------
    private void EnsureParticleExists()
    {
        if (_ps != null) return;
        if (_data == null || _data.waterParticlesPrefab == null) return;

        _psGO = Instantiate(_data.waterParticlesPrefab);
        _ps = _psGO.GetComponent<ParticleSystem>();

        if (_ps != null)
        {
            var main = _ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.stopAction = ParticleSystemStopAction.None;
        }
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_data == null) return;
        Vector3 origin = (_equip != null) ? _equip.position
                    : (_cam != null ? _cam.transform.position : transform.position);

        // 수평 방향
        Vector3 dirH = (_cam != null) ? _cam.transform.forward : transform.forward;
        dirH.y = 0f;
        if (dirH.sqrMagnitude < 1e-6f) dirH = Vector3.forward;
        dirH.Normalize();

        Vector3 p0 = origin;
        Vector3 p1 = origin + dirH * _data.sprayLength;

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.25f);
        Gizmos.DrawWireSphere(p0, _data.sprayRadius);
        Gizmos.DrawWireSphere(p1, _data.sprayRadius);

        // 수평 단면 라인
        Vector3 right = Vector3.Cross(Vector3.up, dirH).normalized;
        Gizmos.DrawLine(p0 + right * _data.sprayRadius, p1 + right * _data.sprayRadius);
        Gizmos.DrawLine(p0 - right * _data.sprayRadius, p1 - right * _data.sprayRadius);
    }
    #endif
}
