using UnityEngine;

public class Fan_Rotate : MonoBehaviour
{
    // public으로 선언하여 유니티 에디터에서 회전 속도를 쉽게 조절할 수 있습니다.
    [Tooltip("초당 회전할 각도입니다.")]
    public float rotateSpeed = 200f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 시작할 때 특별히 해줄 작업은 없습니다.
    }

    // Update is called once per frame
    void Update()
    {
        // Y축을 기준으로 객체를 회전시킵니다.
        // transform.Rotate(x회전, y회전, z회전);
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
    }
}