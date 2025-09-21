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
        if (Time.time - _lastSwingTime < _data.swingCooldown) return;

        int mask = _data.farmPlotMask | _data.groundMask;
        if (!Physics.Raycast(new Ray(_cam.position, _cam.forward), out RaycastHit hit, _data.raycastDistance, mask))
            return;

        // ## 수정: 유효한 타겟(땅 또는 밭)을 찾은 직후 스태미나를 확인하고 소모합니다. ##
        if (!StaminaManager.Instance.UseStamina(_data.staminaCost))
        {
            return; // 스태미나가 부족하면 땅을 파지 않음
        }

        _lastSwingTime = Time.time;
        
        var existingPlot = hit.collider.GetComponentInParent<FarmPlot>();
        if (existingPlot != null)
        {
            existingPlot.AddTill(_data.swingAdd01);
            return;
        }

        if (!Physics.Raycast(new Ray(hit.point + Vector3.up * 2f, Vector3.down),
                             out RaycastHit groundHit, 5f, _data.groundMask))
        {
            groundHit = hit;
        }

        Vector3 spawnPos = groundHit.point;
        if (_data.gridSize > 0f) spawnPos = SnapToGrid(spawnPos, _data.gridSize);
        spawnPos.y = groundHit.point.y + _data.spawnHeightOffset;

        if (!IsSpaceFree(spawnPos, _data.minSeparation, _data.farmPlotMask))
            return;

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
