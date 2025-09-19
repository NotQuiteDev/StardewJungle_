using UnityEngine;

public class TillingHoeRuntime : MonoBehaviour
{
    private float _lastSwingTime = -999f;

    // 클릭 1번 = 스윙 1번
    public void SwingOnce(TillingHoeData data, Transform equip, Transform cam)
    {
        if (data == null || cam == null) return;

        // 쿨다운 체크
        if (Time.time - _lastSwingTime < data.swingCooldown) return;
        _lastSwingTime = Time.time;

        // 1) 카메라 전방으로 레이 쏘기
        int mask = data.farmPlotMask | data.groundMask;
        if (!Physics.Raycast(new Ray(cam.position, cam.forward), out RaycastHit hit, data.raycastDistance, mask))
            return;

        // 2) 맞은 게 이미 경작지면: 생성 없이 Till 추가
        var existingPlot = hit.collider.GetComponentInParent<FarmPlot>();
        if (existingPlot != null)
        {
            existingPlot.AddTill(data.swingAdd01);
            return;
        }

        // 3) 아니면 Ground여야만 생성 (FarmPlotMask에 맞았을 때는 위에서 return 됨)
        //    혹시 레이어가 섞여 있으면 Ground 전용으로 다시 한번 쏴서 안전하게 위치 확보
        if (!Physics.Raycast(new Ray(hit.point + Vector3.up * 2f, Vector3.down),
                             out RaycastHit groundHit, 5f, data.groundMask))
        {
            // 첫 히트가 groundMask였을 수도 있으니, 그 좌표 사용
            groundHit = hit;
        }

        // 4) 스냅/간격/높이 보정
        Vector3 spawnPos = groundHit.point;
        if (data.gridSize > 0f) spawnPos = SnapToGrid(spawnPos, data.gridSize);
        spawnPos.y = groundHit.point.y + data.spawnHeightOffset;

        if (!IsSpaceFree(spawnPos, data.minSeparation, data.farmPlotMask))
        {
            // 주변에 이미 경작지가 있으면 생성하지 않음
            return;
        }

        // 5) 경작지 생성 (정확히 1개만)
        var go = Instantiate(data.farmPlotPrefab, spawnPos, Quaternion.identity);
        var plot = go.GetComponent<FarmPlot>();
        if (plot == null)
        {
            Debug.LogWarning("FarmPlot prefab missing FarmPlot component.");
            return;
        }

        // 6) 스윙 1회분 만큼 경작
        plot.AddTill(data.swingAdd01);
    }

    // --------- helpers ---------
    private static Vector3 SnapToGrid(Vector3 pos, float grid)
    {
        if (grid <= 0f) return pos;
        pos.x = Mathf.Round(pos.x / grid) * grid;
        pos.z = Mathf.Round(pos.z / grid) * grid;
        return pos;
    }

    private static bool IsSpaceFree(Vector3 center, float radius, LayerMask farmPlotMask)
    {
        // 중심에서 반경 검사(플롯 크기가 1x1이면 radius=1이 적당)
        var hits = Physics.OverlapSphere(center, radius, farmPlotMask, QueryTriggerInteraction.Ignore);
        return hits == null || hits.Length == 0;
    }
}
