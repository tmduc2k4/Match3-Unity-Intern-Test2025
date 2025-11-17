using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelMoves : LevelCondition
{
    private int m_moves;
    private BoardController m_board;
    private LayeredBoardController m_layeredBoard;

    // Overload cho LayeredBoardController
    public void Setup(float value, Text txt, LayeredBoardController layeredBoard)
    {
        base.Setup(value, txt);
        m_moves = (int)value;
        m_layeredBoard = layeredBoard;

        if (m_layeredBoard != null)
        {
            m_layeredBoard.OnMoveEvent += OnMove;
        }

        UpdateText();
    }

    // Overload cho BoardController cũ (backward compatibility)
    public override void Setup(float value, Text txt, BoardController board)
    {
        base.Setup(value, txt);
        m_moves = (int)value;
        m_board = board;

        if (m_board != null)
        {
            m_board.OnMoveEvent += OnMove;
        }

        UpdateText();
    }

    private void OnMove()
    {
        if (m_conditionCompleted) return;

        m_moves--;
        UpdateText();

        if (m_moves <= 0)
        {
            OnConditionComplete();
        }
    }

    protected override void UpdateText()
    {
        if (m_txt != null)
        {
            m_txt.text = string.Format("MOVES:\n{0}", m_moves);
        }
    }

    protected override void OnDestroy()
    {
        if (m_board != null)
        {
            m_board.OnMoveEvent -= OnMove;
        }

        if (m_layeredBoard != null)
        {
            m_layeredBoard.OnMoveEvent -= OnMove;
        }

        base.OnDestroy();
    }
}