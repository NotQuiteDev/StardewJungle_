using UnityEngine;
using UnityEngine.InputSystem;

// PlayerInput 컴포넌트가 이 스크립트와 함께 있도록 강제합니다.
[RequireComponent(typeof(PlayerInput))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("상호작용 설정")]
    [Tooltip("상호작용이 가능한 원형 범위의 반경입니다.")]
    [SerializeField] private float interactionRadius = 2f;
    [Tooltip("상호작용 대상을 감지할 레이어입니다.")]
    [SerializeField] private LayerMask interactableLayer;

    private IInteractable closestInteractable; // 감지된 오브젝트 중 가장 가까운 하나만 저장
    private PlayerInput playerInput;
    private InputAction interactAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        // "Player" 액션 맵에서 "Interact" 액션을 이름으로 찾아옵니다.
        interactAction = playerInput.actions["Interact"];
    }

    private void OnEnable()
    {
        // Interact 액션이 수행되었을 때 OnInteract 함수를 호출하도록 구독합니다.
        interactAction.performed += OnInteract;
    }

    private void OnDisable()
    {
        // 스크립트가 비활성화될 때 구독을 해제하여 메모리 누수를 방지합니다.
        interactAction.performed -= OnInteract;
    }

    private void Update()
    {
        // 매 프레임마다 주변에 상호작용 가능한 오브젝트가 있는지 확인합니다.
        CheckForInteractable();
    }

    /// <summary>
    /// Input System에 의해 Interact 액션이 발동될 때 호출되는 함수입니다.
    /// </summary>
/// <summary>
/// Input System에 의해 Interact 액션이 발동될 때 호출되는 함수입니다.
/// </summary>
    private void OnInteract(InputAction.CallbackContext context)
    {
        // ## 핵심 수정: 게임 상태가 'Gameplay'가 아닐 경우, 상호작용을 실행하지 않고 즉시 함수를 탈출! ##
        if (GameManager.Instance.CurrentState != GameManager.GameState.Gameplay) return;

        // 가장 가까운 상호작용 대상이 있을 경우에만 Interact() 함수를 실행합니다.
        if (closestInteractable != null)
        {
            closestInteractable.Interact();
        }
    }

    /// <summary>
    /// 플레이어 주변의 상호작용 가능한 오브젝트를 감지하고 가장 가까운 대상을 찾습니다.
    /// </summary>
    private void CheckForInteractable()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);

        IInteractable foundInteractable = null;
        float minDistance = float.MaxValue;

        if (hitColliders.Length > 0)
        {
            foreach (Collider col in hitColliders)
            {
                // 콜라이더의 부모까지 포함하여 IInteractable 컴포넌트를 찾습니다.
                IInteractable interactable = col.GetComponentInParent<IInteractable>();
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
        
        // 가장 가까운 대상이 변경되었는지 확인하고 UI를 업데이트합니다.
        if (foundInteractable != null && closestInteractable != foundInteractable)
        {
            closestInteractable = foundInteractable;
            // TODO: 상호작용 UI 표시 로직 (예: GameUI.ShowInteractionPrompt(closestInteractable.GetInteractionText()))
        }
        else if (foundInteractable == null && closestInteractable != null)
        {
            closestInteractable = null;
            // TODO: 상호작용 UI 숨기기 로직 (예: GameUI.HideInteractionPrompt())
        }
    }

    // Scene 뷰에서 상호작용 범위를 시각적으로 보여줍니다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}

