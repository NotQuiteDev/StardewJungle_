using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro; // ## 1. TextMeshPro 사용을 위해 추가 ##

public class InventoryManager : MonoBehaviour
{
    [Header("아이템 데이터")]
    public ItemData[] inventorySlotsData = new ItemData[10];

    [Header("UI 관련")]
    public GameObject[] quickSlotsUI;
    public GameObject focusUI;
    
    // ## 2. 배열 타입을 TextMeshProUGUI로 수정 ##
    [Tooltip("각 퀵슬롯 UI 자식에 있는 TextMeshPro 텍스트를 순서대로 연결하세요.")]
    public TextMeshProUGUI[] countTexts = new TextMeshProUGUI[10];

    [Header("플레이어 장착 관련")]
    public Transform equipPoint;
    private GameObject currentEquippedItem;

    private PlayerInput playerInput;
    private InputAction quickSlotSelectAction;
    private InputAction previousAction;
    private InputAction nextAction;
    private int currentFocusIndex = 0;

    [Header("수량(스택)")]
    [SerializeField] private int[] slotCounts = new int[10];


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
    }

    void Start()
    {
        StartCoroutine(InitializeFocusPosition());

        for (int i = 0; i < inventorySlotsData.Length; i++)
        {
            if (inventorySlotsData[i] != null && slotCounts[i] <= 0)
            {
                slotCounts[i] = 1;
            }
        }
        UpdateAllSlotIcons();
        EquipItem(currentFocusIndex);
    }
    
    private void UpdateAllSlotIcons()
    {
        for (int i = 0; i < quickSlotsUI.Length; i++)
        {
            Transform iconTransform = quickSlotsUI[i].transform.Find("ItemIcon");
            if (iconTransform == null) continue;

            var iconImage = iconTransform.GetComponent<Image>();
            var data = (i < inventorySlotsData.Length) ? inventorySlotsData[i] : null;

            if (data != null && data.itemIcon != null)
            {
                iconImage.sprite = data.itemIcon;
                iconImage.color = (data is SeedData sd) ? sd.iconTint : Color.white;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            
            int count = (i < slotCounts.Length) ? slotCounts[i] : 0;
            // ## 변수 타입을 TextMeshProUGUI로 수정 ##
            TextMeshProUGUI currentCountText = (i < countTexts.Length) ? countTexts[i] : null;

            if (currentCountText != null)
            {
                if (data != null && count > 1)
                {
                    currentCountText.text = count.ToString();
                    currentCountText.enabled = true;
                }
                else
                {
                    currentCountText.text = "";
                    currentCountText.enabled = false;
                }
            }
        }
    }
    
    public bool ConsumeFocusedItem(int amount)
    {
        int idx = currentFocusIndex;
        if (idx < 0 || idx >= inventorySlotsData.Length || inventorySlotsData[idx] == null) return false;

        if (slotCounts[idx] < amount) return false;

        slotCounts[idx] -= amount;

        if (slotCounts[idx] <= 0)
        {
            slotCounts[idx] = 0;
            inventorySlotsData[idx] = null;
            if (currentEquippedItem != null)
            {
                Destroy(currentEquippedItem);
                currentEquippedItem = null;
            }
        }

        UpdateAllSlotIcons();
        EquipItem(currentFocusIndex);
        return true;
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