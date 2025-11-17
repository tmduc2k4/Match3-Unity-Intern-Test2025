using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelGameOver : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnClose;
    [SerializeField] private Text txtTitle; // Optional: Hiển thị "YOU WIN!" hoặc "GAME OVER!"
    [SerializeField] private Text txtMessage; // Optional: Message chi tiết
    [SerializeField] private GameObject winContent; // Optional: UI elements cho Win
    [SerializeField] private GameObject loseContent; // Optional: UI elements cho Lose

    private UIMainManager m_mngr;
    private bool m_isWin = false;

    private void Awake()
    {
        if (btnClose) btnClose.onClick.AddListener(OnClickClose);
    }

    private void OnDestroy()
    {
        if (btnClose) btnClose.onClick.RemoveAllListeners();
    }

    private void OnClickClose()
    {
        m_mngr.ShowMainMenu();
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
    }

    public void Show()
    {
        Debug.Log($"[UI PANEL GAME OVER] Show() called. m_isWin={m_isWin}");
        this.gameObject.SetActive(true);
        UpdateUI();
        Debug.Log("[UI PANEL GAME OVER] GameObject activated and UI updated");
    }

    public void SetWin(bool isWin)
    {
        m_isWin = isWin;
    }

    private void UpdateUI()
    {
        // Update title
        if (txtTitle != null)
        {
            txtTitle.text = m_isWin ? "YOU WIN!" : "GAME OVER!";
            txtTitle.color = m_isWin ? Color.green : Color.red;
        }

        // Update message
        if (txtMessage != null)
        {
            txtMessage.text = m_isWin ? "Congratulations! You cleared all tiles!" : "Board is full! Try again.";
        }

        // Show/hide content
        if (winContent != null) winContent.SetActive(m_isWin);
        if (loseContent != null) loseContent.SetActive(!m_isWin);

        Debug.Log($"[UI] UIPanelGameOver shown - IsWin: {m_isWin}");
    }
}
