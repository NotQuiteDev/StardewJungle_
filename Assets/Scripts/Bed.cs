using UnityEngine;

public class Bed : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactText = "Press Interact to Sleep (press again to wake)";
    [SerializeField] private SleepManager sleepManager; // 비우면 자동 Find

    private void Awake()
    {
        if (sleepManager == null) sleepManager = FindObjectOfType<SleepManager>();
    }

    public string GetInteractionText() => interactText;

    public void Interact()
    {
        if (sleepManager == null) return;

        if (!sleepManager.IsSleeping)
        {
            sleepManager.StartSleep();
        }
        else
        {
            // 자는 중이면 같은 상호작용으로 기상 허용 (선택)
            sleepManager.Wake();
        }
    }
}
