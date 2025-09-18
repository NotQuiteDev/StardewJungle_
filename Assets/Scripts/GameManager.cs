// 파일 이름: GameManager.cs (최종 수정본)
using UnityEngine;
using UnityEngine.SceneManagement;
using System; // ★★★ 'Action'을 사용하기 위해 이 줄이 반드시 필요합니다. ★★★

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    // ★★★ UI 업데이트 신호를 보내기 위한 이벤트를 선언합니다. ★★★
    public static event Action OnDeathCountChanged;

    // --- 씬을 넘나들며 유지될 글로벌 데이터 ---
    public int deathCount = 0;

    // --- 각 씬이 시작될 때마다 초기화될 데이터 ---
    public int furBallsCollected = 0;
    
    // (시간 관련 변수는 현재 사용하지 않음)
    public float stageClearTime = 0f;
    private float timer = 0f;
    private bool isTimerRunning = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 로드될 때마다, 털뭉치 개수만 0으로 초기화합니다.
        furBallsCollected = 0;

        // 만약 씬에 GameUI가 있다면, 초기화된 털뭉치 UI를 바로 업데이트해줄 수 있습니다.
        GameUI gameUI = FindObjectOfType<GameUI>();
        if (gameUI != null)
        {
            gameUI.UpdateFurBallUI();
        }
    }

    void Update()
    {
        if (isTimerRunning)
        {
            timer += Time.deltaTime;
        }
    }

    // --- 데이터 관리 함수들 ---

    public void StartTimer()
    {
        timer = 0f;
        isTimerRunning = true;
    }

    public void StopTimer()
    {
        isTimerRunning = false;
        stageClearTime = timer;
    }

    public void RecordDeath()
    {
        deathCount++;

        // ★★★ deathCount가 변경되었음을 UI에게 알리기 위해 신호를 보냅니다. ★★★
        OnDeathCountChanged?.Invoke();
    }

    // "새 게임 시작" 시에만 호출될, 모든 데이터를 초기화하는 함수
    public void ResetAllGameData()
    {
        deathCount = 0;
        furBallsCollected = 0;
        stageClearTime = 0f;
        timer = 0f;

        // 데이터가 0으로 리셋되었을 때도 UI를 업데이트하기 위해 신호를 보냅니다.
        OnDeathCountChanged?.Invoke();
    }
    
    // GameManager가 파괴될 때 이벤트 등록을 해제하여 메모리 누수를 방지합니다.
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}