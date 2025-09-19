using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    // --- UI 관련 변수 ---
    public GameObject[] quickSlots; 
    public GameObject focusUI;      

    // --- 입력 시스템 관련 변수 ---
    private PlayerInput playerInput;
    private InputAction quickSlotSelectAction;
    private InputAction previousAction;
    private InputAction nextAction;

    // --- 인벤토리 상태 변수 ---
    private int currentFocusIndex = 0; 

    void Awake()
    {
        playerInput = FindObjectOfType<PlayerInput>(); 

        // 수정된 부분: "UI" 맵을 특정하지 않고 기본 맵(Player)에서 바로 찾아옵니다.
        quickSlotSelectAction = playerInput.actions.FindAction("QuickSlotSelect");
        previousAction = playerInput.actions.FindAction("Previous");
        nextAction = playerInput.actions.FindAction("Next");
    }

    void OnEnable()
    {
        quickSlotSelectAction.performed += SelectFocusByNumber;
        previousAction.performed += _ => ChangeFocusByStep(-1);
        nextAction.performed += _ => ChangeFocusByStep(1);
    }

    void OnDisable()
    {
        quickSlotSelectAction.performed -= SelectFocusByNumber;
        previousAction.performed -= _ => ChangeFocusByStep(-1);
        nextAction.performed -= _ => ChangeFocusByStep(1);
    }
    
    void Start()
    {
        StartCoroutine(InitializeFocusPosition());
    }

    private IEnumerator InitializeFocusPosition()
    {
        yield return new WaitForEndOfFrame();
        UpdateFocusPosition();
    }
    
    private void SelectFocusByNumber(InputAction.CallbackContext context)
    {
        string keyName = context.control.name;
        int slotNumber = (keyName == "0") ? 10 : int.Parse(keyName);
        currentFocusIndex = slotNumber - 1;

        UpdateFocusPosition();
    }
    
    private void ChangeFocusByStep(int direction)
    {
        currentFocusIndex += direction;

        if (currentFocusIndex < 0)
        {
            currentFocusIndex = quickSlots.Length - 1;
        }
        else if (currentFocusIndex >= quickSlots.Length)
        {
            currentFocusIndex = 0;
        }
        
        UpdateFocusPosition();
    }

    private void UpdateFocusPosition()
    {
        if (currentFocusIndex >= 0 && currentFocusIndex < quickSlots.Length)
        {
            focusUI.transform.position = quickSlots[currentFocusIndex].transform.position;
        }
    }
}