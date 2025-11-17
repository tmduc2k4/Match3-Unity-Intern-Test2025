using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LayeredBoard
{
    private Transform m_root;
    private List<TileCell> m_allCells = new List<TileCell>();

    public LayeredBoard(Transform root)
    {
        m_root = root;
    }

    public void CreateBoard(LevelData levelData)
    {
        Clear();

        GameObject cellPrefab = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);

        if (cellPrefab == null)
        {
            Debug.LogError("Cannot load CellBG prefab!");
            return;
        }

        // Tạo tất cả tiles
        foreach (var tileData in levelData.Tiles)
        {
            GameObject cellGO = GameObject.Instantiate(cellPrefab);
            cellGO.transform.SetParent(m_root);

            // Position với khoảng cách 1.0 unit
            Vector3 pos = new Vector3(tileData.x * 1.0f, tileData.y * 1.0f, 0f);
            cellGO.transform.position = pos;

            TileCell cell = cellGO.GetComponent<TileCell>();
            if (cell == null)
            {
                cell = cellGO.AddComponent<TileCell>();
            }

            cell.Setup(tileData.x, tileData.y, 0);

            NormalItem item = new NormalItem();
            item.SetType(tileData.itemType);
            item.SetView();
            item.SetViewRoot(m_root);

            cell.Assign(item);
            cell.ApplyItemPosition(false);

            m_allCells.Add(cell);
        }

        CalculateBlockingRelationships();
    }

    private void CalculateBlockingRelationships()
    {
        // DISABLED: Không cần logic blocking cho game này
        // Tất cả tiles đều có thể click được

        int availableCount = m_allCells.Count(c => c.IsAvailable);
        Debug.Log($"[BOARD] Created {m_allCells.Count} tiles, ALL {availableCount} are clickable (blocking disabled)");
    }

    // Kiểm tra 2 tiles có overlap không
    private bool IsOverlapping(TileCell bottom, TileCell top)
    {
        float distX = Mathf.Abs(top.BoardX - bottom.BoardX);
        float distY = Mathf.Abs(top.BoardY - bottom.BoardY);

        // Tiles có size 1.0, overlap nếu khoảng cách < 1.0
        return distX < 1.0f && distY < 1.0f;
    }

    public List<TileCell> GetAvailableCells()
    {
        return m_allCells.Where(c => c.IsAvailable).ToList();
    }

    public void OnCellRemoved(TileCell removedCell)
    {
        Debug.Log($"[BOARD] Removing tile at ({removedCell.BoardX}, {removedCell.BoardY})");

        // Không cần xóa blocking references vì không dùng blocking nữa
        m_allCells.Remove(removedCell);

        int availableCount = m_allCells.Count(c => c.IsAvailable);
        Debug.Log($"[BOARD] {m_allCells.Count} tiles left, {availableCount} clickable");
    }

    public bool IsEmpty()
    {
        return m_allCells.Count == 0 || m_allCells.All(c => c.IsEmpty);
    }

    public bool HasAvailableMoves()
    {
        return GetAvailableCells().Count > 0;
    }

    public void Clear()
    {
        foreach (var cell in m_allCells)
        {
            if (cell != null)
            {
                cell.Clear();
                GameObject.Destroy(cell.gameObject);
            }
        }

        m_allCells.Clear();
    }

    public int GetRemainingItemCount()
    {
        return m_allCells.Count(c => !c.IsEmpty);
    }

    public int GetTopLayer()
    {
        return 0;
    }
}