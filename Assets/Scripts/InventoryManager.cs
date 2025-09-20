using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro; // ## 1. TextMeshPro 사용을 위해 추가 ##

public class InventoryManager : MonoBehaviour
{
    // ## 추가 1: 싱글턴 인스턴스 변수 ##
    public static InventoryManager Instance { get; private set; }
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

    // ## 추가: 아이템 드랍 설정 ##
    [Header("아이템 드랍 설정")]
    [Tooltip("바닥에 생성될 아이템 드랍 프리팹 (ItemDropShell)")]
    [SerializeField] private GameObject itemDropPrefab;
    [Tooltip("아이템을 앞으로 밀어내는 힘")]
    [SerializeField] private float dropForce = 3f;

    private PlayerInput playerInput;
    private InputAction quickSlotSelectAction;
    private InputAction previousAction;
    private InputAction nextAction;
    private InputAction dropItemAction; // ## 추가 ##
    private int currentFocusIndex = 0;

    [Header("수량(스택)")]
    [SerializeField] public int[] slotCounts = new int[10];


    void Awake()
    {
        // ## 추가 2: 싱글턴 로직 ##
        if (Instance == null)
        {
            Instance = this;
            // 인벤토리는 보통 플레이어에 붙어있으므로 DontDestroyOnLoad는 플레이어 생명주기에 맡김
        }
        else
        {
            Destroy(gameObject);
            return; // 중복 생성을 막고 아래 로직 실행 방지
        }

        playerInput = FindObjectOfType<PlayerInput>();
        quickSlotSelectAction = playerInput.actions.FindAction("QuickSlotSelect");
        previousAction = playerInput.actions.FindAction("Previous");
        nextAction = playerInput.actions.FindAction("Next");
        dropItemAction = playerInput.actions.FindAction("DropItem"); // ## 추가: DropItem 액션 찾기 ##
    }

    void OnEnable()
    {
        quickSlotSelectAction.performed += SelectFocusByNumber;
        previousAction.performed += _ => ChangeFocusByStep(-1);
        nextAction.performed += _ => ChangeFocusByStep(1);
        dropItemAction.performed += DropFocusedItem; // ## 추가: DropItem 액션 구독 ##
    }

    void OnDisable()
    {
        quickSlotSelectAction.performed -= SelectFocusByNumber;
        dropItemAction.performed -= DropFocusedItem; // ## 추가: 구독 해제 ##
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
    /// <summary>
    /// 아이템 데이터와 수량을 지정하여 인벤토리에서 제거합니다. 판매 시 사용됩니다.
    /// </summary>
    public bool RemoveItem(ItemData itemToRemove, int count)
    {
        for (int i = 0; i < inventorySlotsData.Length; i++)
        {
            if (inventorySlotsData[i] == itemToRemove)
            {
                if (slotCounts[i] >= count)
                {
                    slotCounts[i] -= count;
                    if (slotCounts[i] <= 0)
                    {
                        inventorySlotsData[i] = null;
                        // 현재 장착된 아이템이 판매한 아이템이면 장착 해제
                        if (i == currentFocusIndex)
                        {
                            EquipItem(i);
                        }
                    }
                    UpdateAllSlotIcons(); // UI 갱신
                    return true;
                }
                else
                {
                    // 수량이 부족하면 실패
                    return false;
                }
            }
        }
        return false; // 해당 아이템이 인벤토리에 없음
    }
    /// <summary>
    /// ## 추가: 현재 들고 있는 아이템을 바닥에 드랍하는 함수 ##
    /// </summary>
    private void DropFocusedItem(InputAction.CallbackContext context)
    {
        // 1. 현재 들고 있는 아이템 정보 가져오기
        ItemData itemToDrop = GetCurrentFocusedItem();
        if (itemToDrop == null) return; // 빈 슬롯이면 아무것도 안 함

        // 2. 드랍 프리팹이 설정되었는지 확인
        if (itemDropPrefab == null)
        {
            Debug.LogError("Item Drop Prefab이 InventoryManager에 연결되지 않았습니다!");
            return;
        }

        // 3. 아이템 생성 위치 계산 (플레이어 정면 살짝 위)
        Vector3 spawnPosition = equipPoint.position + equipPoint.forward * 1.0f;

        // 4. 아이템 드랍 프리팹 생성
        GameObject dropInstance = Instantiate(itemDropPrefab, spawnPosition, Quaternion.identity);

        // 5. 생성된 아이템에 정보 주입 (어떤 아이템인지, 1개인지)
        dropInstance.GetComponent<ItemDrop>().Initialize(itemToDrop, 1);
        
        // 6. 생성된 아이템에 물리적 힘을 가해 앞으로 던지기
        Rigidbody rb = dropInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(equipPoint.forward * dropForce, ForceMode.Impulse);
        }

        // 7. 인벤토리에서 아이템 1개 소모
        ConsumeFocusedItem(1);
    }

}