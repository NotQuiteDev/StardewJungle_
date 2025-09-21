using UnityEngine;
using System.Collections;

public class PickaxeRuntime : MonoBehaviour
{
    private PickaxeData _data;
    private Transform _cam;

    private bool _isMining = false;
    private Coroutine _holdLoop;
    private float _lastSwingTime = -999f;

    public void BeginMining(PickaxeData data, Transform equip, Transform cam)
    {
        _data = data;
        _cam = cam;

        DoSwing(); // 클릭 즉시 1회 휘두르기

        _isMining = true;
        if (_holdLoop == null && isActiveAndEnabled)
            _holdLoop = StartCoroutine(HoldLoop());
    }

    public void StopMining()
    {
        _isMining = false;
        if (_holdLoop != null)
        {
            StopCoroutine(_holdLoop);
            _holdLoop = null;
        }
    }
    
    private void OnDisable()
    {
        StopMining();
    }

    private IEnumerator HoldLoop()
    {
        while (_isMining)
        {
            // 쿨다운이 지났으면 추가로 휘두름
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
        
        _lastSwingTime = Time.time;

        // 카메라 정면으로 레이를 쏴서 MineableStone을 찾음
        if (Physics.Raycast(new Ray(_cam.position, _cam.forward), out RaycastHit hit, _data.raycastDistance, _data.mineableMask))
        {
            var stone = hit.collider.GetComponent<MineableStone>();
            if (stone != null)
            {
                // 돌을 찾았으면 데미지를 줌
                stone.TakeDamage(_data.damagePerSwing);
                
                // 여기에 타격 사운드나 이펙트 재생 로직을 추가하면 좋습니다.
            }
        }
    }
}