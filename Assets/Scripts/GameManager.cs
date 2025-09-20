using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Gameplay, UI }
    public GameState CurrentState { get; private set; }

    // 게임 상태가 변경될 때 다른 스크립트에게 알리는 방송 신호
    public static event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 게임을 'UI 모드'로 전환합니다. (시간 정지, 마우스 활성화)
    /// </summary>
    public void EnterUIMode()
    {
        CurrentState = GameState.UI;
        Time.timeScale = 0f; // 시간 정지
        Cursor.lockState = CursorLockMode.None; // 마우스 잠금 해제
        Cursor.visible = true; // 마우스 보이기
        OnGameStateChanged?.Invoke(GameState.UI);
        Debug.Log("게임 상태: UI 모드 진입");
    }

    /// <summary>
    /// 게임을 '게임플레이 모드'로 전환합니다.
    /// </summary>
    public void EnterGameplayMode()
    {
        CurrentState = GameState.Gameplay;
        Time.timeScale = 1f; // 시간 정상화
        Cursor.lockState = CursorLockMode.Locked; // 마우스 잠금
        Cursor.visible = false; // 마우스 숨기기
        OnGameStateChanged?.Invoke(GameState.Gameplay);
        Debug.Log("게임 상태: 게임플레이 모드 진입");
    }
}