using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelMain : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnTimer;

    [SerializeField] private Button btnMoves;

    private UIMainManager m_mngr;

    private void Awake()
    {
        if (btnMoves != null)
        {
            btnMoves.onClick.AddListener(OnClickMoves);
        }
        else
        {
            Debug.LogWarning("[UIPanelMain] btnMoves is not assigned in Inspector!");
        }

        if (btnTimer != null)
        {
            btnTimer.onClick.AddListener(OnClickTimer);
        }
        else
        {
            Debug.LogWarning("[UIPanelMain] btnTimer is not assigned in Inspector!");
        }
    }

    private void OnDestroy()
    {
        if (btnMoves) btnMoves.onClick.RemoveAllListeners();
        if (btnTimer) btnTimer.onClick.RemoveAllListeners();
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
    }

    private void OnClickTimer()
    {
        m_mngr.LoadLevelTimer();
    }

    private void OnClickMoves()
    {
        m_mngr.LoadLevelMoves();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
