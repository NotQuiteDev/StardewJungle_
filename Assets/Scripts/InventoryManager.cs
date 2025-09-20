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
    /// <summary>
    /// 인벤토리에 아이템을 추가합니다. 성공하면 true, 공간이 없어 실패하면 false를 반환합니다.
    /// </summary>
    public bool AddItem(ItemData itemToAdd, int count)
    {
        // 1. 스택 가능한 아이템인지 확인하고, 이미 인벤토리에 있는지 찾기
        for (int i = 0; i < inventorySlotsData.Length; i++)
        {
            // 같은 아이템이 있고, 그 아이템이 스택이 가능하다면 (예: isStackable 같은 속성 필요)
            // 여기서는 모든 아이템이 스택 가능하다고 가정하겠습니다.
            if (inventorySlotsData[i] == itemToAdd)
            {
                slotCounts[i] += count;
                UpdateAllSlotIcons(); // UI 갱신
                Debug.Log($"{itemToAdd.itemName} {count}개 획득 (스택 추가)");
                return true; // 성공
            }
        }

        // 2. 스택할 아이템이 없다면, 빈 슬롯 찾기
        for (int i = 0; i < inventorySlotsData.Length; i++)
        {
            if (inventorySlotsData[i] == null)
            {
                inventorySlotsData[i] = itemToAdd;
                slotCounts[i] = count;
                UpdateAllSlotIcons(); // UI 갱신
                Debug.Log($"{itemToAdd.itemName} {count}개 획득 (새 슬롯)");
                return true; // 성공
            }
        }

        // 3. 빈 슬롯도 없으면 실패
        Debug.Log("인벤토리가 가득 찼습니다!");
        return false;
    }

}