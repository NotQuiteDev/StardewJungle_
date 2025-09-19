using UnityEngine;
using System.Collections;

public class TillingHoeRuntime : MonoBehaviour
{
    private TillingHoeData _data;
    private Transform _equip;
    private Transform _cam;

    private bool _isTilling = false;
    private Coroutine _holdLoop;
    private float _lastSwingTime = -999f;

    // ===== 외부 API =====
    public void BeginTilling(TillingHoeData data, Transform equip, Transform cam)
    {
        _data  = data;
        _equip = equip;
        _cam   = cam;

        // 탭 즉시 1회
        DoSwing();

        // 홀드 시작
        _isTilling = true;
        if (_holdLoop == null && isActiveAndEnabled)
            _holdLoop = StartCoroutine(HoldLoop());
    }

    public void StopTilling()
    {
        _isTilling = false;
        if (_holdLoop != null)
        {
            StopCoroutine(_holdLoop);
            _holdLoop = null;
        }
    }

    // ===== 생명주기 안전 처리 =====
    private void OnDisable()
    {
        _isTilling = false;
        if (_holdLoop != null)
        {
            StopCoroutine(_holdLoop);
            _holdLoop = null;
        }
    }

    // ===== 내부 =====
    private IEnumerator HoldLoop()
    {
        while (_isTilling && this != null && isActiveAndEnabled)
        {
            // 쿨다운 지난 경우에만 추가 스윙
            if (Time.time - _lastSwingTime >= _data.swingCooldown)
            {
                DoSwing();
            }
            yield return null;
        }
        _holdLoop = null;
    }

    private void DoSwing()
    {
        if (_data == null || _cam == null) return;

        // 쿨다운 체크 (탭-스팸 방지)
        if (Time.time - _lastSwingTime < _data.swingCooldown) return;
        _lastSwingTime = Time.time;

        // 1) 카메라 전방 Ray (FarmPlot 또는 Ground)
        int mask = _data.farmPlotMask | _data.groundMask;
        if (!Physics.Raycast(new Ray(_cam.position, _cam.forward), out RaycastHit hit, _data.raycastDistance, mask))
            return;

        // 2) 이미 경작지면: 생성 없이 Till 추가
        var existingPlot = hit.collider.GetComponentInParent<FarmPlot>();
        if (existingPlot != null)
        {
            existingPlot.AddTill(_data.swingAdd01);
            return;
        }

        // 3) Ground 확정(안전 재캐스트)
        if (!Physics.Raycast(new Ray(hit.point + Vector3.up * 2f, Vector3.down),
                             out RaycastHit groundHit, 5f, _data.groundMask))
        {
            groundHit = hit; // 첫 히트가 Ground였을 수 있음
        }

        // 4) 스냅/간격/높이 보정
        Vector3 spawnPos = groundHit.point;
        if (_data.gridSize > 0f) spawnPos = SnapToGrid(spawnPos, _data.gridSize);
        spawnPos.y = groundHit.point.y + _data.spawnHeightOffset;

        if (!IsSpaceFree(spawnPos, _data.minSeparation, _data.farmPlotMask))
            return; // 근처에 이미 있으면 생성 금지

        // 5) 정확히 1개 생성 후 1회 Till
        var go = Object.Instantiate(_data.farmPlotPrefab, spawnPos, Quaternion.identity);
        var plot = go.GetComponent<FarmPlot>();
        if (plot == null)
        {
            Debug.LogWarning("FarmPlot prefab missing FarmPlot component.");
            return;
        }
        plot.AddTill(_data.swingAdd01);
    }

    private static Vector3 SnapToGrid(Vector3 pos, float grid)
    {
        if (grid <= 0f) return pos;
        pos.x = Mathf.Round(pos.x / grid) * grid;
        pos.z = Mathf.Round(pos.z / grid) * grid;
        return pos;
    }

    private static bool IsSpaceFree(Vector3 center, float radius, LayerMask farmPlotMask)
    {
        var hits = Physics.OverlapSphere(center, radius, farmPlotMask, QueryTriggerInteraction.Ignore);
        return hits == null || hits.Length == 0;
    }
}
