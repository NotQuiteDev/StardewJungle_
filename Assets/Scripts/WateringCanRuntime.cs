using UnityEngine;
using System.Collections;

public class WateringCanRuntime : MonoBehaviour
{
    private Coroutine _loop;
    private ParticleSystem _ps;
    private Transform _equip;
    private Transform _cam;
    private WateringCanData _data;

    // 파티클 오브젝트 (부모 없음, 월드 스페이스)
    private GameObject _psGO;

    // 시작: 데이터/장착점/카메라 세팅 + 루프
    public void StartWatering(WateringCanData data, Transform equip, Transform cam)
    {
        _data = data;
        _equip = equip;
        _cam = cam;

        if (_ps == null && _data.waterParticlesPrefab != null)
        {
            // ※ 부모를 붙이지 말자. (부모 비활성/파괴 시 메모리 에러 방지)
            _psGO = Instantiate(_data.waterParticlesPrefab, _equip.position, _equip.rotation);
            _ps = _psGO.GetComponent<ParticleSystem>();

            // 안전장치: Simulation Space는 World여야 한다.
            var main = _ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            _ps.Play();
        }

        if (_loop == null) _loop = StartCoroutine(WaterLoop());
    }

    public void StopWatering()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
        StartCoroutine(StopAndCleanupParticles());
    }

    private IEnumerator StopAndCleanupParticles()
    {
        if (_ps != null)
        {
            // 먼저 방출 중지
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            // 모든 살아있는 파티클이 사라질 때까지 대기 (안전)
            // (최대 대기시간 방어)
            float timeout = 1.0f;
            while (_ps.IsAlive(true) && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            // 완전 정리
            _ps.Clear(true);
        }

        // 파괴 순서는 마지막에
        if (_psGO != null)
        {
            Destroy(_psGO);
            _psGO = null;
        }
        _ps = null;
    }

    private IEnumerator WaterLoop()
    {
        while (true)
        {
            // 파티클 오브젝트가 살아 있다면, 장착 위치/방향만 추적
            if (_psGO != null && _equip != null)
            {
                _psGO.transform.position = _equip.position;
                _psGO.transform.rotation = _equip.rotation;
            }

            // 레이캐스트 → 초당량 * deltaTime 물주기
            if (_cam != null)
            {
                Ray ray = new Ray(_cam.position, _cam.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, _data.raycastDistance))
                {
                    var crop = hit.collider.GetComponentInParent<CropManager>();
                    if (crop != null)
                    {
                        float amount = _data.waterPerSecond * Time.deltaTime;
                        crop.WaterCrop(amount);
                    }
                }
            }
            yield return null;
        }
    }

    private void OnDisable()
    {
        // 비활성화 시에도 안전 정리
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
        // Stop 순서를 지켜서 크래시 방지
        StartCoroutine(StopAndCleanupParticles());
    }

    private void OnDestroy()
    {
        // 추가 안전망
        if (_ps != null)
        {
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _ps.Clear(true);
        }
        if (_psGO != null)
        {
            Destroy(_psGO);
        }
    }
}
