using UnityEngine;
using System.Linq; // OrderBy를 사용하기 위해 필요

public class PlayerInteraction : MonoBehaviour
{
    [Header("상호작용 설정")]
    [Tooltip("상호작용이 가능한 원형 범위의 반경입니다.")]
    [SerializeField] private float interactionRadius = 2f;
    [Tooltip("상호작용 대상을 감지할 레이어입니다.")]
    [SerializeField] private LayerMask interactableLayer; // ★★★ 추가: 특정 레이어만 감지하여 최적화 ★★★

    private IInteractable closestInteractable; // 감지된 오브젝트 중 가장 가까운 하나만 저장

    void Update()
    {
        // 1. 내 주변에 상호작용 가능한 오브젝트가 있는지 계속 확인한다.
        CheckForInteractable();

        // 2. 가장 가까운 상호작용 대상이 있고, E키를 눌렀다면 작동시킨다.
        if (closestInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            closestInteractable.Interact();
        }
    }

    private void CheckForInteractable()
    {
        // 1. 플레이어 위치를 중심으로, interactionRadius 반경 안에 들어온 모든 콜라이더를 찾는다.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);

        IInteractable foundInteractable = null;
        float minDistance = float.MaxValue;

        // 2. 찾아낸 모든 콜라이더들 중에서 가장 가까운 것을 찾는다.
        if (hitColliders.Length > 0)
        {
            foreach (Collider col in hitColliders)
            {
                // 콜라이더에서 IInteractable 컴포넌트를 찾아본다.
                IInteractable interactable = col.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        foundInteractable = interactable;
                    }
                }
            }
        }
        
        // 3. 가장 가까운 대상이 바뀌었는지 확인하고 UI를 업데이트한다.
        // 이전에 대상이 없다가 새로 찾았거나, 다른 대상을 찾았을 경우
        if (foundInteractable != null && closestInteractable != foundInteractable)
        {
            closestInteractable = foundInteractable;
            FindObjectOfType<GameUI>()?.ShowInteractionPrompt(closestInteractable.GetInteractionText());
        }
        // 이전에 대상이 있었는데 이제는 아무것도 찾지 못했을 경우
        else if (foundInteractable == null && closestInteractable != null)
        {
            closestInteractable = null;
            FindObjectOfType<GameUI>()?.HideInteractionPrompt();
        }
    }

    // Scene 뷰에서 상호작용 범위를 시각적으로 보여줌
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}