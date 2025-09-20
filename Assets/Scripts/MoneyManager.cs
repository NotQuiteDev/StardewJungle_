using UnityEngine;
using System;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }
    public event Action<int> OnMoneyChanged;

    [Header("초기 자금")]
    [SerializeField] private int startingMoney = 100;

    public int CurrentMoney { get; private set; }

    private void Awake()
    {
        // --- 싱글턴 설정 ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ## ★★★★★ 핵심 수정 부분 ★★★★★ ##
            // 돈 초기화를 Start()가 아닌 Awake()에서 먼저 처리합니다.
            CurrentMoney = startingMoney;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 시작하자마자 UI가 올바른 값을 표시할 수 있도록 신호를 보냅니다.
        // Awake에서 이미 값이 설정되었으므로, UI는 이 신호를 받고 즉시 업데이트됩니다.
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    public void AddMoney(int amount)
    {
        if (amount < 0) return;
        CurrentMoney += amount;
        Debug.Log($"{amount}원 획득! 현재 잔액: {CurrentMoney}원");
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    public bool SpendMoney(int amount)
    {
        if (amount < 0) return false;
        if (CurrentMoney >= amount)
        {
            CurrentMoney -= amount;
            Debug.Log($"{amount}원 사용. 현재 잔액: {CurrentMoney}원");
            OnMoneyChanged?.Invoke(CurrentMoney);
            return true;
        }
        else
        {
            Debug.Log("잔액이 부족합니다.");
            return false;
        }
    }
}