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

    // 메인 동작 코루틴 및 상태
    private Coroutine _loop;
    private bool _isWatering = false;
    
    // ## 삭제: 더 이상 '틱' 방식의 누적 변수는 필요 없습니다. ##
    // private float staminaUsageAccumulator = 0f;

    // 물리 히트 & 크롭 버퍼(고정 크기, GC 없음)
    private const int MaxHits = 64;
    private static readonly Collider[] _hits = new Collider[MaxHits];
    private static readonly CropManager[] _crops = new CropManager[MaxHits];

    [Header("Visual")]
    [SerializeField] private bool forceHorizontalParticleDirection = true;
    
    public void StartWatering(WateringCanData data, Transform equip, Transform cam)
    {
        if (StaminaManager.Instance.CurrentStamina <= 0) return;

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
    
    private IEnumerator WaterLoop()
    {
        while (true)
        {
            if (_isWatering && _cam != null && _data != null)
            {
                // ## 핵심 수정: 매 프레임마다 사용한 시간만큼 스태미나를 소모합니다. ##
                // 1. 이번 프레임에 소모할 스태미나 양을 계산합니다.
                float staminaToConsume = _data.staminaCost * Time.deltaTime;

                // 2. 스태미나를 소모하고, 실패하면 물뿌리개를 즉시 멈춥니다.
                if (!StaminaManager.Instance.UseStamina(staminaToConsume))
                {
                    StopWatering();
                    continue; // 이번 프레임의 나머지 로직(물주기)은 건너뜁니다.
                }

                // (스태미나 소모에 성공했을 때만 아래 로직이 실행됩니다)
                UpdateParticleTransform();
                ApplyWaterToCrops();
            }
            yield return null;
        }
    }

    private void ApplyWaterToCrops()
    {
        Vector3 origin = (_equip != null) ? _equip.position : _cam.position;
        Vector3 dirH = HorizontalDir(_cam, _equip);
        Vector3 p0 = origin;
        Vector3 p1 = origin + dirH * _data.sprayLength;

        int layerMask = (_data.waterableLayer.value == 0) ? Physics.DefaultRaycastLayers : _data.waterableLayer.value;
        int hitCount = Physics.OverlapCapsuleNonAlloc(p0, p1, _data.sprayRadius, _hits, layerMask);
        
        int uniqueCount = 0;
        for (int i = 0; i < hitCount && uniqueCount < MaxHits; i++)
        {
            var crop = _hits[i]?.GetComponentInParent<CropManager>();
            if (crop == null) continue;
            bool seen = false;
            for (int k = 0; k < uniqueCount; k++) { if (_crops[k] == crop) { seen = true; break; } }
            if (!seen) _crops[uniqueCount++] = crop;
        }

        float dtAmount = _data.waterPerSecond * Time.deltaTime;
        if (uniqueCount > 0)
        {
            float each = _data.distributeWaterEvenly ? (dtAmount / uniqueCount) : dtAmount;
            for (int i = 0; i < uniqueCount; i++)
            {
                _crops[i]?.WaterCrop(each);
                _crops[i] = null;
            }
        }
    }

    private void UpdateParticleTransform()
    {
        if (_psGO != null && _equip != null)
        {
            _psGO.transform.position = _equip.position;
            if (forceHorizontalParticleDirection && _cam != null)
            {
                 _psGO.transform.rotation = Quaternion.LookRotation(HorizontalDir(_cam, _equip), Vector3.up);
            }
            else
            {
                _psGO.transform.rotation = _equip.rotation;
            }
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
        StopWatering();
        if (_ps != null) _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    private void OnDestroy()
    {
        StopWatering();
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
        if (_psGO != null)
        {
            Destroy(_psGO);
        }
    }
    
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
}