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
    private bool _isWatering = false;   // 디바운스용
    private bool _loopRequested = false;

    // -----------------------
    // 공개 API
    // -----------------------
    public void StartWatering(WateringCanData data, Transform equip, Transform cam)
    {
        _data  = data;
        _equip = equip;
        _cam   = cam;

        EnsureParticleExists();

        if (_ps != null)
        {
            // 노즐 위치/방향 동기화
            if (_equip != null)
            {
                _psGO.transform.position = _equip.position;
                _psGO.transform.rotation = _equip.rotation;
            }

            // ★★ 핵심: 빠른 재시작을 위해 하드 리셋
            _ps.Clear(true);                               // 버퍼 초기화
            var emission = _ps.emission; emission.enabled = true;
            if (!_ps.isPlaying) _ps.Play(true);            // 즉시 재생
        }

        _isWatering = true; // 루프는 항상 돌고 있으므로, 이 플래그만 켠다
    }


    public void StopWatering()
    {
        if (!_isWatering) return;
        _isWatering = false;

        if (_ps != null)
        {
            // 즉시 방출 중지. Clear는 하지 말고(바로 재시작 대비), 루프에서 필요 시 Start에서 Clear
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }


    // -----------------------
    // 내부 루프
    // -----------------------
    private System.Collections.IEnumerator WaterLoop()
    {
        while (true)
        {
            // 파티클 노즐 위치/회전 추적
            if (_psGO != null && _equip != null)
            {
                _psGO.transform.position = _equip.position;
                _psGO.transform.rotation = _equip.rotation;
            }

            // 물 공급
            if (_isWatering && _cam != null && _data != null)
            {
                Ray ray = new Ray(_cam.position, _cam.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, _data.raycastDistance))
                {
                    var crop = hit.collider.GetComponentInParent<CropManager>();
                    if (crop != null)
                    {
                        crop.WaterCrop(_data.waterPerSecond * Time.deltaTime);
                    }
                }
            }

            yield return null;
        }
    }


    private void OnEnable()
    {
        if (_loop == null) _loop = StartCoroutine(WaterLoop());
    }

    private void OnDisable()
    {
        _isWatering = false;

        if (_ps != null)
        {
            // 즉시 멈추고 잔여 파티클도 정리
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    private void OnDestroy()
    {
        // 최종 정리(여기서만 파괴)
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
        _ps   = _psGO.GetComponent<ParticleSystem>();

        if (_ps != null)
        {
            var main = _ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // 중요
            main.loop = true;                                          // 중요
            main.stopAction = ParticleSystemStopAction.None;
        }
    }

}
