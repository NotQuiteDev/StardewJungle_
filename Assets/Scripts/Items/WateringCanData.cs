using UnityEngine;

[CreateAssetMenu(fileName = "New Watering Can", menuName = "Inventory/Watering Can")]
public class WateringCanData : ItemData
{
    [Header("물뿌리개 전용 설정")]
    public GameObject waterParticlesPrefab;
    public float waterAmount = 5f;
    public float raycastDistance = 5f;

    // 변경된 인자에 맞춰 Use 함수 수정
    public override void Use(Transform equipPoint, Transform cameraTransform)
    {
        // 1. 물 파티클 생성
        if (waterParticlesPrefab != null && equipPoint != null)
        {
            Instantiate(waterParticlesPrefab, equipPoint.position, equipPoint.rotation);
        }

        // 2. 전달받은 카메라 위치에서 레이캐스트 발사
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            // 3. 맞은 오브젝트가 CropManager를 가지고 있는지 확인
            CropManager crop = hit.collider.GetComponent<CropManager>();
            if (crop != null)
            {
                // 4. CropManager가 있다면 물주기 함수 호출
                crop.WaterCrop(waterAmount);
                Debug.Log($"{crop.name}에 물을 {waterAmount}만큼 주었습니다.");
            }
        }
    }
}