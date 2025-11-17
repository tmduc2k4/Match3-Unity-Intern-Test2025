using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIMainManager : MonoBehaviour
{
    [Header("Game Panels (Optional - for direct win/lose display)")]
    [SerializeField] private GameObject panelWin;
    [SerializeField] private GameObject panelGameOver;

    private IMenu[] m_menuList;
    private GameManager m_gameManager;

    private void Awake()
    {
        m_menuList = GetComponentsInChildren<IMenu>(true);
    }

    void Start()
    {
        for (int i = 0; i < m_menuList.Length; i++)
        {
            m_menuList[i].Setup(this);
        }
    }

    internal void ShowMainMenu()
    {
        m_gameManager.ClearLevel();
        m_gameManager.SetState(GameManager.eStateGame.MAIN_MENU);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_gameManager.State == GameManager.eStateGame.GAME_STARTED)
            {
                m_gameManager.SetState(GameManager.eStateGame.PAUSE);
            }
            else if (m_gameManager.State == GameManager.eStateGame.PAUSE)
            {
                m_gameManager.SetState(GameManager.eStateGame.GAME_STARTED);
            }
        }
    }

    internal void Setup(GameManager gameManager)
    {
        m_gameManager = gameManager;
        m_gameManager.StateChangedAction += OnGameStateChange;
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.SETUP:
                break;
            case GameManager.eStateGame.MAIN_MENU:
                ShowMenu<UIPanelMain>();
                break;
            case GameManager.eStateGame.GAME_STARTED:
                ShowMenu<UIPanelGame>();
                break;
            case GameManager.eStateGame.PAUSE:
                ShowMenu<UIPanelPause>();
                break;
            case GameManager.eStateGame.GAME_OVER:
                ShowMenu<UIPanelGameOver>();
                break;
        }
    }

    private void ShowMenu<T>() where T : IMenu
    {
        for (int i = 0; i < m_menuList.Length; i++)
        {
            IMenu menu = m_menuList[i];
            if (menu is T)
            {
                menu.Show();
            }
            else
            {
                menu.Hide();
            }
        }
    }

    internal Text GetLevelConditionView()
    {
        UIPanelGame game = m_menuList.Where(x => x is UIPanelGame).Cast<UIPanelGame>().FirstOrDefault();
        if (game)
        {
            return game.LevelConditionView;
        }
        return null;
    }

    internal void ShowPauseMenu()
    {
        m_gameManager.SetState(GameManager.eStateGame.PAUSE);
    }

    internal void LoadLevelMoves()
    {
        m_gameManager.LoadLevel(GameManager.eLevelMode.MOVES);
    }

    internal void LoadLevelTimer()
    {
        m_gameManager.LoadLevel(GameManager.eLevelMode.TIMER);
    }

    internal void ShowGameMenu()
    {
        m_gameManager.SetState(GameManager.eStateGame.GAME_STARTED);
    }

    // ===== THÊM 2 METHODS MỚI =====

    public void ShowWinPanel()
    {
        // Cách 1: Hiện panel trực tiếp (nếu có assign trong Inspector)
        if (panelWin != null)
        {
            panelWin.SetActive(true);
            Debug.Log("[UI] Win panel shown (direct)");
            return;
        }

        // Cách 2: Tìm UIPanelGameOver và set IsWin = true
        UIPanelGameOver gameOverPanel = m_menuList
            .Where(x => x is UIPanelGameOver)
            .Cast<UIPanelGameOver>()
            .FirstOrDefault();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetWin(true);
            gameOverPanel.Show();
            Debug.Log("[UI] Win panel shown via UIPanelGameOver");
        }
        else
        {
            Debug.LogWarning("[UI] No Win panel found! Add panelWin to Inspector or use UIPanelGameOver");
        }
    }

    public void ShowGameOverPanel()
    {
        Debug.Log("[UI MANAGER] ShowGameOverPanel() called");

        // Cách 1: Hiện panel trực tiếp (nếu có assign trong Inspector)
        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
            Debug.Log("[UI MANAGER] Game over panel shown (direct)");
            return;
        }

        // Cách 2: Dùng UIPanelGameOver (đã có sẵn từ state machine)
        UIPanelGameOver gameOverPanel = m_menuList
            .Where(x => x is UIPanelGameOver)
            .Cast<UIPanelGameOver>()
            .FirstOrDefault();

        if (gameOverPanel != null)
        {
            Debug.Log("[UI MANAGER] Found UIPanelGameOver, setting IsWin=false");
            gameOverPanel.SetWin(false);
            gameOverPanel.Show();
            Debug.Log("[UI MANAGER] UIPanelGameOver.Show() called");
        }
        else
        {
            Debug.LogWarning("[UI MANAGER] No Game Over panel found! Add panelGameOver to Inspector");
        }
    }

    private void OnDestroy()
    {
        if (m_gameManager != null)
        {
            m_gameManager.StateChangedAction -= OnGameStateChange;
        }
    }
}