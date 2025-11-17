using System;
using UnityEngine;

public class LayeredBoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };
    public event Action OnLevelComplete = delegate { };
    public event Action OnLevelFailed = delegate { };

    public bool IsBusy { get; private set; }

    [Header("Board Settings")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private int defaultTileCount = 18; // Phải chia hết cho 3

    [Header("Bottom Slots")]
    [SerializeField] private BottomSlotsManager bottomSlotsManager;

    private LayeredBoard board;
    private GameManager gameManager;
    private LevelData currentLevel;
    private Camera cam;
    private bool gameOver;

    public void StartGame(GameManager gm, LevelData level = null)
    {
        Debug.LogError("🔴🔴🔴 CODE MỚI ĐÃ ĐƯỢC COMPILE! 🔴🔴🔴");
        gameManager = gm;
        currentLevel = level;

        gameManager.StateChangedAction += OnGameStateChange;
        cam = Camera.main;

        if (boardRoot == null)
            boardRoot = this.transform;

        board = new LayeredBoard(boardRoot);

        // Nếu không có level data, tạo level 1 tầng
        if (currentLevel == null || !currentLevel.IsValid())
        {
            Debug.LogWarning("Creating random single-layer level");
            currentLevel = LevelData.CreateRandomLevel(1, 1, defaultTileCount);
        }

        board.CreateBoard(currentLevel);

        // Setup bottom slots manager
        if (bottomSlotsManager == null)
        {
            bottomSlotsManager = FindObjectOfType<BottomSlotsManager>();
        }

        if (bottomSlotsManager == null)
        {
            // Tự động tạo BottomSlotsManager nếu không tìm thấy
            Debug.Log("[BOARD CONTROLLER] Auto-creating BottomSlotsManager");
            GameObject slotsGO = new GameObject("BottomSlotsManager");
            bottomSlotsManager = slotsGO.AddComponent<BottomSlotsManager>();

            // Đặt vị trí ở dưới màn hình
            slotsGO.transform.position = new Vector3(0, -4, 0);
        }

        if (bottomSlotsManager != null)
        {
            bottomSlotsManager.OnSlotsFull += OnBottomSlotsFull;
            bottomSlotsManager.OnItemsMatched += OnItemsMatched;
        }

        IsBusy = false;
        gameOver = false;

        Debug.Log($"[BOARD CONTROLLER] Started with {defaultTileCount} tiles (1 layer)");
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                gameOver = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                gameOver = true;
                break;
        }
    }

    private void Update()
    {
        if (gameOver || IsBusy) return;

        if (Input.GetMouseButtonDown(0))
            HandleTapInput();
    }

    private void HandleTapInput()
    {
        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            TileCell cell = hit.collider.GetComponent<TileCell>();

            if (cell != null && cell.IsAvailable)
            {
                Debug.Log($"[TAP] Clicked tile at ({cell.BoardX}, {cell.BoardY})");
                OnTileClicked(cell);
            }
        }
    }

    private void OnTileClicked(TileCell cell)
    {
        if (cell.Item == null)
        {
            Debug.LogError("Cell has no item!");
            return;
        }

        if (bottomSlotsManager == null)
        {
            Debug.LogError("BottomSlotsManager is null!");
            return;
        }

        if (bottomSlotsManager.IsFull)
        {
            Debug.LogWarning("Bottom slots are full!");
            return;
        }

        IsBusy = true;

        Item itemToMove = cell.Item;

        // Add item to bottom slots with animation
        bottomSlotsManager.AddItem(itemToMove, () =>
        {
            // After animation completes, remove from board
            Debug.Log("[BOARD CONTROLLER] onComplete callback: freeing cell and removing from board");
            cell.Free();
            board.OnCellRemoved(cell);
            IsBusy = false;
            OnMoveEvent();

            // IMPORTANT: Only check win if game is not over yet
            // gameOver is set by OnBottomSlotsFull (lose) or CheckWinCondition (win)
            Debug.Log($"[BOARD CONTROLLER] gameOver={gameOver}, checking if should call CheckWinCondition");
            if (!gameOver)
            {
                Debug.Log("[BOARD CONTROLLER] Calling CheckWinCondition...");
                CheckWinCondition();
            }
            else
            {
                Debug.Log("[BOARD CONTROLLER] Skipping CheckWinCondition because gameOver=true");
            }
        });
    }

    private void CheckWinCondition()
    {
        if (board != null && board.IsEmpty())
        {
            Debug.Log("🎉 LEVEL COMPLETE - All tiles cleared!");
            gameOver = true;
            OnLevelComplete();
        }
    }

    private void OnBottomSlotsFull()
    {
        Debug.Log("❌❌❌ [BOARD CONTROLLER] OnBottomSlotsFull called! Setting IsBusy=false, gameOver=true");
        IsBusy = false; // IMPORTANT: Set IsBusy to false so GameManager can proceed
        gameOver = true;
        Debug.Log("[BOARD CONTROLLER] Calling OnLevelFailed event...");
        OnLevelFailed();
        Debug.Log("[BOARD CONTROLLER] OnLevelFailed event fired!");
    }

    private void OnItemsMatched(int count)
    {
        Debug.Log($"✨ Matched {count} items!");
        // Có thể thêm score, effects, sounds, etc.
    }

    public void Clear()
    {
        if (board != null)
            board.Clear();

        if (bottomSlotsManager != null)
        {
            bottomSlotsManager.OnSlotsFull -= OnBottomSlotsFull;
            bottomSlotsManager.OnItemsMatched -= OnItemsMatched;
            bottomSlotsManager.Clear();
        }
    }

    public void RestartLevel()
    {
        Clear();

        if (currentLevel != null && boardRoot != null)
        {
            board = new LayeredBoard(boardRoot);
            board.CreateBoard(currentLevel);
        }

        // Re-subscribe to bottom slots events
        if (bottomSlotsManager != null)
        {
            bottomSlotsManager.OnSlotsFull += OnBottomSlotsFull;
            bottomSlotsManager.OnItemsMatched += OnItemsMatched;
        }

        IsBusy = false;
        gameOver = false;
    }

    public string GetBoardInfo()
    {
        if (board == null) return "Board not initialized";

        int remainingItems = board.GetRemainingItemCount();
        int availableMoves = board.GetAvailableCells().Count;

        return $"Items: {remainingItems} | Available: {availableMoves} | Layer: 0";
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.StateChangedAction -= OnGameStateChange;
        }

        if (bottomSlotsManager != null)
        {
            bottomSlotsManager.OnSlotsFull -= OnBottomSlotsFull;
            bottomSlotsManager.OnItemsMatched -= OnItemsMatched;
        }
    }
}