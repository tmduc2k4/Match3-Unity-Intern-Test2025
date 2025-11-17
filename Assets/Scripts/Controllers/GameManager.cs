using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };

    public enum eLevelMode { TIMER, MOVES }
    public enum eStateGame { SETUP, MAIN_MENU, GAME_STARTED, PAUSE, GAME_OVER }

    private eStateGame m_state;
    public eStateGame State
    {
        get => m_state;
        private set
        {
            m_state = value;
            StateChangedAction(m_state);
        }
    }

    public bool IsWin { get; private set; }

    [Header("Settings")]
    private GameSettings m_gameSettings;

    [Header("Level")]
    [SerializeField] private int m_currentLevelNumber = 1;
    [SerializeField] private LevelData m_currentLevelData; // Optional: assign trong Inspector

    [Header("Controllers")]
    private LayeredBoardController m_boardController;
    private UIMainManager m_uiMenu;
    private LevelCondition m_levelCondition;

    private void Awake()
    {
        State = eStateGame.SETUP;
        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        m_uiMenu = FindObjectOfType<UIMainManager>();
        if (m_uiMenu != null) m_uiMenu.Setup(this);
    }

    private void Start() => State = eStateGame.MAIN_MENU;

    internal void SetState(eStateGame state)
    {
        State = state;
        if (State == eStateGame.PAUSE) DOTween.PauseAll();
        else DOTween.PlayAll();
    }

    public void LoadLevel(eLevelMode mode)
    {
        // Xóa board cũ
        ClearLevel();

        // Tạo board mới
        GameObject boardGO = new GameObject("BoardController");
        m_boardController = boardGO.AddComponent<LayeredBoardController>();

        // Load level data
        LevelData levelData = GetLevelData(m_currentLevelNumber);

        // Start game với level data
        m_boardController.StartGame(this, levelData);

        // Subscribe events
        m_boardController.OnLevelComplete += OnLevelComplete;
        m_boardController.OnLevelFailed += OnLevelFailed;

        // Setup level condition
        if (mode == eLevelMode.MOVES && m_uiMenu != null)
        {
            m_levelCondition = gameObject.AddComponent<LevelMoves>();
            ((LevelMoves)m_levelCondition).Setup(
                m_gameSettings.LevelMoves,
                m_uiMenu.GetLevelConditionView(),
                m_boardController
            );
        }
        else if (mode == eLevelMode.TIMER && m_uiMenu != null)
        {
            m_levelCondition = gameObject.AddComponent<LevelTime>();
            m_levelCondition.Setup(
                m_gameSettings.LevelTime,
                m_uiMenu.GetLevelConditionView(),
                this
            );
        }

        if (m_levelCondition != null)
            m_levelCondition.ConditionCompleteEvent += GameOver;

        State = eStateGame.GAME_STARTED;
        IsWin = false;
    }

    private LevelData GetLevelData(int levelNumber)
    {
        // 1. Thử load từ Resources/Levels/
        LevelData levelData = Resources.Load<LevelData>($"Levels/Level_{levelNumber:D2}");
        if (levelData != null && levelData.IsValid())
        {
            Debug.Log($"Loaded Level_{levelNumber:D2} from Resources");
            return levelData;
        }

        // 2. Thử dùng level data từ Inspector
        if (m_currentLevelData != null && m_currentLevelData.IsValid())
        {
            Debug.Log("Using level data from Inspector");
            return m_currentLevelData;
        }

        // 3. Tạo random level 1 tầng
        Debug.LogWarning($"Generating random single-layer level {levelNumber}");
        return LevelData.CreateRandomLevel(levelNumber, 1, 18); // 1 tầng, 18 tiles
    }

    private void OnLevelComplete()
    {
        Debug.Log("🎉 LEVEL COMPLETE!");
        IsWin = true;
        GameOver();
    }

    private void OnLevelFailed()
    {
        Debug.Log("❌❌❌ [GAME MANAGER] OnLevelFailed called!");
        IsWin = false;
        Debug.Log("[GAME MANAGER] Calling GameOver()...");
        GameOver();
    }

    public void GameOver()
    {
        Debug.Log("[GAME MANAGER] GameOver() called, starting WaitBoardController coroutine");
        StartCoroutine(WaitBoardController());
    }

    internal void ClearLevel()
    {
        if (m_boardController != null)
        {
            m_boardController.OnLevelComplete -= OnLevelComplete;
            m_boardController.OnLevelFailed -= OnLevelFailed;
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;
            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }

    private IEnumerator WaitBoardController()
    {
        Debug.Log("[GAME MANAGER] WaitBoardController coroutine started");

        // Đợi board controller xử lý xong
        while (m_boardController != null && m_boardController.IsBusy)
        {
            Debug.Log($"[GAME MANAGER] Waiting for board controller... IsBusy={m_boardController.IsBusy}");
            yield return new WaitForEndOfFrame();
        }

        Debug.Log("[GAME MANAGER] Board controller finished (IsBusy=false), waiting 1 second...");
        yield return new WaitForSeconds(1f);

        Debug.Log($"[GAME MANAGER] Setting State to GAME_OVER. IsWin={IsWin}");
        State = eStateGame.GAME_OVER;

        // Show UI panel
        if (m_uiMenu != null)
        {
            if (IsWin)
            {
                Debug.Log("[GAME MANAGER] 🎉 Calling ShowWinPanel");
                m_uiMenu.ShowWinPanel();
            }
            else
            {
                Debug.Log("[GAME MANAGER] ❌ Calling ShowGameOverPanel");
                m_uiMenu.ShowGameOverPanel();
            }
        }
        else
        {
            Debug.LogError("[GAME MANAGER] m_uiMenu is NULL!");
        }

        // Cleanup level condition
        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;
            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }

    public void RestartLevel()
    {
        if (m_boardController != null)
        {
            m_boardController.RestartLevel();
            State = eStateGame.GAME_STARTED;
            IsWin = false;
        }
    }

    public void LoadNextLevel()
    {
        m_currentLevelNumber++;
        ClearLevel();
        LoadLevel(eLevelMode.MOVES);
    }

    public void SetLevelNumber(int levelNumber)
    {
        m_currentLevelNumber = levelNumber;
    }

    private void OnDestroy()
    {
        if (m_boardController != null)
        {
            m_boardController.OnLevelComplete -= OnLevelComplete;
            m_boardController.OnLevelFailed -= OnLevelFailed;
        }

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;
        }
    }
}