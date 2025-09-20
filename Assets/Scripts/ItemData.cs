using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [Header("공통 정보")]
    public string itemName;
    public Sprite itemIcon;
    public GameObject itemPrefab;
    public int sellPrice;

    // 단발 사용 (그대로 유지)
    public abstract void Use(Transform equipPoint, Transform cameraTransform);

    // 홀드형 사용 시작/종료 (기본은 아무 일도 안 함)
    public virtual void BeginUse(Transform equipPoint, Transform cameraTransform, MonoBehaviour runner) {}
    public virtual void EndUse() {}
}
