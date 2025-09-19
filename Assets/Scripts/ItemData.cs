using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [Header("공통 정보")]
    public string itemName;
    public Sprite itemIcon;
    public GameObject itemPrefab;

    // Use 함수의 인자를 더 명확하게 변경!
    public abstract void Use(Transform equipPoint, Transform cameraTransform);
}