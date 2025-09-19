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

    [Header("수량(스택)")]
    [SerializeField] private int[] slotCounts = new int[10]; // 슬롯 수와 동일 크기


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
        // 데이터가 있고 카운트가 0이면 1로
        for (int i = 0; i < inventorySlotsData.Length; i++)
            if (inventorySlotsData[i] != null && slotCounts[i] <= 0) slotCounts[i] = 1;

        UpdateAllSlotIcons();
        EquipItem(currentFocusIndex);
    }

    // 아이콘을 업데이트하는 함수 (수정된 부분!)
    private void UpdateAllSlotIcons()
    {
        for (int i = 0; i < quickSlotsUI.Length; i++)
        {
            Transform iconTransform = quickSlotsUI[i].transform.Find("ItemIcon");
            if (iconTransform == null) continue;

            var iconImage = iconTransform.GetComponent<Image>();
            var countTextTr = quickSlotsUI[i].transform.Find("Count"); // TextMeshProUGUI 추천 (없으면 null)
            Text countText = countTextTr ? countTextTr.GetComponent<Text>() : null;
            // (Text 대신 TMP를 쓰면 타입만 바꾸세요)

            var data = (i < inventorySlotsData.Length) ? inventorySlotsData[i] : null;

            if (data != null && data.itemIcon != null)
            {
                iconImage.sprite = data.itemIcon;

                // ★ 씨앗이면 틴트, 아니면 흰색
                if (data is SeedData sd) iconImage.color = sd.iconTint;
                else iconImage.color = Color.white;

                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            // 수량 표시 (선택)
            int cnt = (i < slotCounts.Length) ? slotCounts[i] : 0;
            if (countText != null)
            {
                countText.text = (data != null && cnt > 1) ? cnt.ToString() : ""; // 1 이하면 비움
            }
        }
    }

    public bool ConsumeFocusedItem(int amount)
    {
        int idx = currentFocusIndex;
        if (idx < 0 || idx >= inventorySlotsData.Length) return false;
        if (inventorySlotsData[idx] == null) return false;

        int have = slotCounts[idx];
        if (have < amount) return false;

        slotCounts[idx] = have - amount;

        if (slotCounts[idx] <= 0)
        {
            // 아이템 소진 → 슬롯 비움 + 장착 해제
            slotCounts[idx] = 0;
            inventorySlotsData[idx] = null;

            if (currentEquippedItem != null)
            {
                Destroy(currentEquippedItem);
                currentEquippedItem = null;
            }
        }

        UpdateAllSlotIcons();
        // 장착 오브젝트는 data가 null이면 자연히 없음. 안전하게 일치시킬려면:
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