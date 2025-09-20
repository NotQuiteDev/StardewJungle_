// ItemDrop.cs
using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    [Header("아이템 정보")]
    public ItemData itemData; // 획득할 아이템의 정보
    public int count;         // 획득할 개수

    [Header("내부 연결")]
    [SerializeField] private Transform modelAnchor; // 아이템 모델이 생성될 위치

    [Header("애니메이션")]
    [SerializeField] private float floatSpeed = 0.5f;  // 위아래로 움직이는 속도
    [SerializeField] private float floatHeight = 0.25f; // 위아래로 움직이는 높이

    private Vector3 basePosition;
    private GameObject spawnedModel; // 생성된 모델을 저장할 변수

    void Start()
    {
        // 시작 위치를 저장해둡니다.
        basePosition = modelAnchor.localPosition;
    }

    void Update()
    {
        // Sin 함수를 이용해 부드러운 상하 움직임 생성
        float newY = basePosition.y + Mathf.Sin(Time.time * Mathf.PI * floatSpeed) * floatHeight;
        modelAnchor.localPosition = new Vector3(basePosition.x, newY, basePosition.z);
    }

    /// <summary>
    /// 이 드랍 아이템을 초기화하고, 내용물 모델을 생성합니다.
    /// </summary>
    public void Initialize(ItemData data, int amount)
    {
        itemData = data;
        count = amount;

        // 이전에 생성된 모델이 있다면 파괴
        if (spawnedModel != null)
        {
            Destroy(spawnedModel);
        }

        // ItemData에 연결된 itemPrefab이 있는지 확인
        if (itemData != null && itemData.itemPrefab != null)
        {
            // 모델을 ModelAnchor의 자식으로 생성
            spawnedModel = Instantiate(itemData.itemPrefab, modelAnchor);
            spawnedModel.transform.localPosition = Vector3.zero; // 앵커의 정중앙에 위치

            // 중요: 드랍된 아이템의 모델은 그냥 '장식'이므로, 기능적인 스크립트나 콜라이더는 비활성화!
            // 이렇게 하지 않으면 손에 드는 무기의 공격 스크립트가 바닥에서도 실행될 수 있음.
            Collider[] colliders = spawnedModel.GetComponentsInChildren<Collider>();
            foreach (var col in colliders) col.enabled = false;

            // 추가적으로 비활성화할 커스텀 스크립트가 있다면 여기에 추가
            // 예: if (spawnedModel.GetComponent<Weapon>() != null) spawnedModel.GetComponent<Weapon>().enabled = false;
        }
    }
}