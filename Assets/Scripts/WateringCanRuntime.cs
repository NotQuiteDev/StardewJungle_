using UnityEngine;
using System.Collections;

public class WateringCanRuntime : MonoBehaviour
{
    private Coroutine _loop;
    private ParticleSystem _ps;
    private Transform _equip;
    private Transform _cam;
    private WateringCanData _data;

    // 시작: 데이터/장착점/카메라 정보 세팅 + 루프 가동
    public void StartWatering(WateringCanData data, Transform equip, Transform cam)
    {
        _data = data;
        _equip = equip;
        _cam = cam;

        if (_loop == null) _loop = StartCoroutine(WaterLoop());
    }

    // 종료: 루프와 파티클 정지
    public void StopWatering()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
        if (_ps != null)
        {
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private IEnumerator WaterLoop()
    {
        // 파티클 인스턴스 1회 생성(장착점 자식으로 붙임)
        if (_data.waterParticlesPrefab != null && _equip != null && _ps == null)
        {
            var go = Instantiate(_data.waterParticlesPrefab, _equip.position, _equip.rotation, _equip);
            _ps = go.GetComponent<ParticleSystem>();
            if (_ps != null) _ps.Play();
        }

        // 매 프레임: 레이캐스트 → 맞으면 초당량 * deltaTime 만큼 물 주기
        while (true)
        {
            if (_ps != null)
            {
                _ps.transform.position = _equip.position;
                _ps.transform.rotation = _equip.rotation;
            }

            Ray ray = new Ray(_cam.position, _cam.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _data.raycastDistance))
            {
                // 자식/루트 어디 붙었든 안전하게 찾기
                var crop = hit.collider.GetComponentInParent<CropManager>();
                if (crop != null)
                {
                    float amount = _data.waterPerSecond * Time.deltaTime;
                    crop.WaterCrop(amount);
#if UNITY_EDITOR
                    // 디버깅용(원하면 끄기)
                    // Debug.Log($"{crop.name} +{amount:F3}");
#endif
                }
            }

            yield return null; // 다음 프레임
        }
    }
}
