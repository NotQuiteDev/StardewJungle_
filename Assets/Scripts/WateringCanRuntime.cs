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

    // ## '틱' 방식 스태미나 소모를 위한 누적 변수 ##
    private float staminaUsageAccumulator = 0f;

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
        // 스태미나가 0 이하면 아예 시작하지 않도록 방지
        if (StaminaManager.Instance.CurrentStamina <= 0)
        {
            return;
        }

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
        staminaUsageAccumulator = 0f; // 사용 시작 시 누적기 초기화
    }

    public void StopWatering()
    {
        if (!_isWatering) return;
        _isWatering = false;
        staminaUsageAccumulator = 0f; // 사용 중지 시 누적기 초기화

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
            // 물을 뿌리는 중일 때만 아래 로직 실행
            if (_isWatering && _cam != null && _data != null)
            {
                // 파티클 비주얼 업데이트
                UpdateParticleTransform();

                // ## 새로운 '틱' 방식 스태미나 소모 로직 ##
                // 1. 매 프레임 물을 사용한 시간을 누적합니다.
                staminaUsageAccumulator += Time.deltaTime;

                // 2. 누적 시간이 1초를 넘으면 스태미나를 소모합니다.
                if (staminaUsageAccumulator >= 1f)
                {
                    staminaUsageAccumulator -= 1f; // 정확히 1초만큼만 차감 (0.05초 등 나머지 시간은 유지)
                    
                    if (!StaminaManager.Instance.UseStamina(_data.staminaCost))
                    {
                        StopWatering(); // 스태미나가 부족하면 물뿌리기를 강제로 멈춥니다.
                        continue;       // 이번 프레임의 물주기 로직은 건너뜁니다.
                    }
                }
                
                // 작물에 물을 적용하는 로직
                ApplyWaterToCrops();
            }

            yield return null; // 다음 프레임까지 대기
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

        // 중복되지 않는 CropManager 수집
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

        // 물 적용
        float dtAmount = _data.waterPerSecond * Time.deltaTime;
        if (uniqueCount > 0)
        {
            float each = _data.distributeWaterEvenly ? (dtAmount / uniqueCount) : dtAmount;
            for (int i = 0; i < uniqueCount; i++)
            {
                var crop = _crops[i];
                if (crop != null) crop.WaterCrop(each);
                _crops[i] = null; // 다음 프레임을 위해 버퍼 비우기
            }
        }
        else
        {
            // 백업용 단일 레이캐스트
            Ray ray = new Ray(origin, dirH);
            if (Physics.Raycast(ray, out RaycastHit hit, _data.raycastDistance, layerMask))
            {
                var crop = hit.collider.GetComponentInParent<CropManager>();
                if (crop != null) crop.WaterCrop(dtAmount);
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

    // -----------------------
    // 유니티 생명주기 함수
    // -----------------------
    private void OnEnable()
    {
        if (_loop == null) _loop = StartCoroutine(WaterLoop());
    }

    private void OnDisable()
    {
        StopWatering(); // 비활성화될 때 무조건 중지

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
        // 오브젝트 파괴 시 모든 것 정리
        StopWatering();
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
        
        if (_psGO != null)
        {
            Destroy(_psGO);
            _psGO = null;
            _ps = null;
        }
    }

    // -----------------------
    // 유틸리티
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
        // (기즈모 코드는 디버깅용이므로 기존과 동일하게 유지)
    }
    #endif
}