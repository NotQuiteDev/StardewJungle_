using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("아이템 데이터")]
    public ItemData[] inventorySlotsData = new ItemData[10];

    [Header("UI 관련")]
    public GameObject[] quickSlotsUI;
    public GameObject focusUI;

    [Header("플레이어 장착 관련")]
    public Transform equipPoint;
    private GameObject currentEquippedItem;

    // --- 입력 시스템 변수 ---
    private PlayerInput playerInput;
    private InputAction quickSlotSelectAction;
    private InputAction previousAction;
    private InputAction nextAction;
    private int currentFocusIndex = 0;

    void Awake()
    {
        playerInput = FindObjectOfType<PlayerInput>();
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
        UpdateAllSlotIcons();
        EquipItem(currentFocusIndex);
    }

    // 아이콘을 업데이트하는 함수 (수정된 부분!)
    private void UpdateAllSlotIcons()
    {
        for (int i = 0; i < quickSlotsUI.Length; i++)
        {
            // 슬롯의 자식 중에서 "ItemIcon"을 이름으로 찾아 그 Image 컴포넌트를 가져옵니다.
            Transform iconTransform = quickSlotsUI[i].transform.Find("ItemIcon");
            if (iconTransform == null) continue; // ItemIcon 자식이 없으면 건너뜀

            Image iconImage = iconTransform.GetComponent<Image>();

            // 데이터가 있으면 아이콘을 활성화하고 이미지를 바꿉니다.
            if (inventorySlotsData[i] != null && inventorySlotsData[i].itemIcon != null)
            {
                iconImage.sprite = inventorySlotsData[i].itemIcon;
                iconImage.enabled = true; // 이미지 컴포넌트 활성화
            }
            // 데이터가 없으면 아이콘을 비활성화합니다.
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false; // 이미지 컴포넌트 비활성화
            }
        }
    }

    private void UpdateFocusPosition()
    {
        if (currentFocusIndex >= 0 && currentFocusIndex < quickSlotsUI.Length)
        {
            focusUI.transform.position = quickSlotsUI[currentFocusIndex].transform.position;
            EquipItem(currentFocusIndex);
        }
    }

    private void EquipItem(int slotIndex)
    {
        if (currentEquippedItem != null)
        {
            Destroy(currentEquippedItem);
        }

        ItemData itemToEquip = inventorySlotsData[slotIndex];
        if (itemToEquip != null && itemToEquip.itemPrefab != null)
        {
            currentEquippedItem = Instantiate(itemToEquip.itemPrefab, equipPoint);
        }
    }
    public ItemData GetCurrentFocusedItem()
    {
        if (currentFocusIndex >= 0 && currentFocusIndex < inventorySlotsData.Length)
        {
            return inventorySlotsData[currentFocusIndex];
        }
        return null;
    }
    
    // --- 아래는 기존 포커스 이동 관련 함수들 (수정 없음) ---
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
        if (currentFocusIndex < 0) { currentFocusIndex = quickSlotsUI.Length - 1; }
        else if (currentFocusIndex >= quickSlotsUI.Length) { currentFocusIndex = 0; }
        UpdateFocusPosition();
    }
}