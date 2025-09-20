using UnityEngine;
using TMPro; // TextMeshPro 사용을 위해 필요

public class MoneyDisplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;

    private void OnEnable()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged += UpdateMoneyText;
        }
    }

    private void OnDisable()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyText;
        }
    }

    private void Start()
    {
        if (MoneyManager.Instance != null)
        {
            UpdateMoneyText(MoneyManager.Instance.CurrentMoney);
        }
    }

    /// <summary>
    /// MoneyManager로부터 신호를 받으면 호출되는 함수입니다.
    /// </summary>
    private void UpdateMoneyText(int newAmount)
    {
        // ## ★★★★★ 핵심 수정 부분 ★★★★★ ##
        // $"" 문법을 사용해 원화(₩) 기호와 세 자리 쉼표(,)를 포함하여 포매팅합니다.
        moneyText.text = $"$ {newAmount:N0}";
        // ===================================
    }
}