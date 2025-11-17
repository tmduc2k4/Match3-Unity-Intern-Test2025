using System.Collections.Generic;
using UnityEngine;

public class TileCell : MonoBehaviour
{
    public int BoardX { get; private set; }
    public int BoardY { get; private set; }
    public int Layer { get; private set; }

    public Item Item { get; private set; }

    private List<TileCell> m_blockingCellsAbove = new List<TileCell>();
    private List<TileCell> m_neighboursLeft = new List<TileCell>();
    private List<TileCell> m_neighboursRight = new List<TileCell>();

    private GameObject m_whiteBackground;
    private GameObject m_glowEffect;

    public bool IsAvailable => Item != null;

    public bool IsEmpty => Item == null;

    private SpriteRenderer m_spriteRenderer;

    private void Awake()
    {
        m_spriteRenderer = GetComponent<SpriteRenderer>();
        CreateVisualElements();
    }

    private void CreateVisualElements()
    {
        // Tạo glow effect (hiệu ứng sáng xung quanh)
        m_glowEffect = new GameObject("Glow");
        m_glowEffect.transform.SetParent(this.transform);
        m_glowEffect.transform.localPosition = Vector3.zero;

        SpriteRenderer glowSpr = m_glowEffect.AddComponent<SpriteRenderer>();
        Texture2D glowTex = new Texture2D(120, 120);
        Color[] glowPixels = new Color[120 * 120];

        // Tạo gradient từ giữa ra ngoài (glow effect)
        for (int y = 0; y < 120; y++)
        {
            for (int x = 0; x < 120; x++)
            {
                float centerX = 60f;
                float centerY = 60f;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                float maxDist = 60f;

                float alpha = Mathf.Clamp01(1f - (dist / maxDist));
                alpha = alpha * alpha; // Falloff mượt hơn

                glowPixels[y * 120 + x] = new Color(1f, 1f, 0.8f, alpha * 0.4f); // Vàng nhạt
            }
        }

        glowTex.SetPixels(glowPixels);
        glowTex.Apply();

        Sprite glowSprite = Sprite.Create(glowTex, new Rect(0, 0, 120, 120), new Vector2(0.5f, 0.5f), 100);
        glowSpr.sprite = glowSprite;
        glowSpr.sortingOrder = -2;
        m_glowEffect.SetActive(false);

        // Tạo white background (tile border)
        m_whiteBackground = new GameObject("Background");
        m_whiteBackground.transform.SetParent(this.transform);
        m_whiteBackground.transform.localPosition = Vector3.zero;

        SpriteRenderer bgSpr = m_whiteBackground.AddComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(100, 100);
        Color[] pixels = new Color[100 * 100];
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % 100;
            int y = i / 100;

            // Border trắng dày 4 pixels
            if (x < 4 || x > 96 || y < 4 || y > 96)
            {
                pixels[i] = Color.white;
            }
            else
            {
                pixels[i] = new Color(0.95f, 0.95f, 0.95f, 1f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        Sprite whiteSprite = Sprite.Create(tex, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f), 100);
        bgSpr.sprite = whiteSprite;
        bgSpr.sortingOrder = -1;

        m_whiteBackground.SetActive(false);
    }

    public void Setup(int x, int y, int layer)
    {
        this.BoardX = x;
        this.BoardY = y;
        this.Layer = layer;

        // Clear blocking lists - không dùng blocking nữa
        m_blockingCellsAbove.Clear();
        m_neighboursLeft.Clear();
        m_neighboursRight.Clear();

        // Sorting order: tiles ở hàng trên (Y lớn hơn) render trên tiles ở hàng dưới
        if (m_spriteRenderer != null)
        {
            m_spriteRenderer.sortingOrder = (y * 100) + x;
        }

        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
    }

    public void Assign(Item item)
    {
        Item = item;

        // Clear blocking lists - không dùng blocking nữa
        m_blockingCellsAbove.Clear();
        m_neighboursLeft.Clear();
        m_neighboursRight.Clear();

        if (Item != null && Item.View != null)
        {
            SpriteRenderer itemSpr = Item.View.GetComponent<SpriteRenderer>();
            if (itemSpr != null)
            {
                // Item render trên background
                itemSpr.sortingOrder = (BoardY * 100) + BoardX + 1;
            }

            Item.View.localScale = Vector3.one * 0.85f;
        }

        UpdateVisualState();
    }

    public void Free()
    {
        Item = null;
    }

    public void ApplyItemPosition(bool withAnimation)
    {
        if (Item == null) return;

        Item.SetViewPosition(this.transform.position);
        if (withAnimation)
        {
            Item.ShowAppearAnimation();
        }
    }

    public void AddBlockingCellAbove(TileCell blockingCell)
    {
        if (!m_blockingCellsAbove.Contains(blockingCell))
        {
            m_blockingCellsAbove.Add(blockingCell);
        }
        UpdateVisualState();
    }

    public void RemoveBlockingCellAbove(TileCell blockingCell)
    {
        m_blockingCellsAbove.Remove(blockingCell);
        UpdateVisualState();
    }

    public void AddNeighbourLeft(TileCell neighbour)
    {
        if (!m_neighboursLeft.Contains(neighbour))
        {
            m_neighboursLeft.Add(neighbour);
        }
        UpdateVisualState();
    }

    public void AddNeighbourRight(TileCell neighbour)
    {
        if (!m_neighboursRight.Contains(neighbour))
        {
            m_neighboursRight.Add(neighbour);
        }
        UpdateVisualState();
    }

    public void RemoveNeighbourLeft(TileCell neighbour)
    {
        m_neighboursLeft.Remove(neighbour);
        UpdateVisualState();
    }

    public void RemoveNeighbourRight(TileCell neighbour)
    {
        m_neighboursRight.Remove(neighbour);
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (Item == null || Item.View == null) return;

        SpriteRenderer itemSpr = Item.View.GetComponent<SpriteRenderer>();

        if (IsAvailable)
        {
            // Tile CÓ THỂ CLICK: Sáng + Glow + Border trắng
            if (m_whiteBackground != null)
            {
                m_whiteBackground.SetActive(true);
            }

            if (m_glowEffect != null)
            {
                m_glowEffect.SetActive(true);
            }

            if (itemSpr != null)
            {
                itemSpr.color = Color.white; // Sáng đầy đủ
                Item.View.localScale = Vector3.one * 0.85f;
            }
        }
        else
        {
            // Tile BỊ BLOCK: Tối + Không glow + Không border
            if (m_whiteBackground != null)
            {
                m_whiteBackground.SetActive(false);
            }

            if (m_glowEffect != null)
            {
                m_glowEffect.SetActive(false);
            }

            if (itemSpr != null)
            {
                itemSpr.color = new Color(0.4f, 0.4f, 0.4f, 0.6f); // Tối mờ
                Item.View.localScale = Vector3.one * 0.7f; // Nhỏ hơn
            }
        }
    }

    public void RemoveItem(System.Action<TileCell> onItemRemoved)
    {
        if (Item != null)
        {
            Item.ExplodeView();
            Item = null;
        }

        onItemRemoved?.Invoke(this);
    }

    public void Clear()
    {
        if (Item != null)
        {
            Item.Clear();
            Item = null;
        }

        if (m_whiteBackground != null)
        {
            Destroy(m_whiteBackground);
        }

        if (m_glowEffect != null)
        {
            Destroy(m_glowEffect);
        }

        m_blockingCellsAbove.Clear();
        m_neighboursLeft.Clear();
        m_neighboursRight.Clear();
    }

    public bool IsLeftNeighbour(TileCell other)
    {
        return other.BoardX < this.BoardX &&
               Mathf.Abs(other.BoardY - this.BoardY) < 0.5f &&
               Mathf.Abs(other.BoardX - this.BoardX) <= 1;
    }

    public bool IsRightNeighbour(TileCell other)
    {
        return other.BoardX > this.BoardX &&
               Mathf.Abs(other.BoardY - this.BoardY) < 0.5f &&
               Mathf.Abs(other.BoardX - this.BoardX) <= 1;
    }
}