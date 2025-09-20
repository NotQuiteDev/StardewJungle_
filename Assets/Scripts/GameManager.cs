using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public enum GameState { Gameplay, UI }
    public GameState CurrentState { get; private set; }
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

    public void EnterUIMode()
    {
        CurrentState = GameState.UI;
        Time.timeScale = 0.001f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        OnGameStateChanged?.Invoke(GameState.UI);
    }

    public void EnterGameplayMode()
    {
        CurrentState = GameState.Gameplay;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        OnGameStateChanged?.Invoke(GameState.Gameplay);
    }
}