using UnityEngine;
using TMPro;

public class MoneyDisplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;

    private void OnEnable()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyText;
            MoneyManager.Instance.OnMoneyChanged += UpdateMoneyText;
            UpdateMoneyText(MoneyManager.Instance.CurrentMoney);
        }
    }

    private void OnDisable()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyText;
        }
    }

    private void UpdateMoneyText(int newAmount)
    {
        if (moneyText != null)
        {
            moneyText.text = $"$ {newAmount:N0}";
        }
    }

    /// <summary>
    /// 외부에서 UI를 강제로 새로고침하도록 명령하는 함수
    /// </summary>
    public void ForceUpdate()
    {
        if (MoneyManager.Instance != null)
        {
             UpdateMoneyText(MoneyManager.Instance.CurrentMoney);
        }
    }
}