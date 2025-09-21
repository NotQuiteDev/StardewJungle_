using UnityEngine;

// ItemData를 상속받는 ToolData를 만듭니다.
public abstract class ToolData : ItemData
{
    [Header("도구 공통 설정")]
    [Tooltip("한 번 사용할 때 소모되는 스태미나 양")]
    public float staminaCost = 1f;
}