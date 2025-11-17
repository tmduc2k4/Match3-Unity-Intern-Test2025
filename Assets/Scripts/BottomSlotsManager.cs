using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

public class BottomSlotsManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int m_maxSlots = 5;
    [SerializeField] private float m_slotSpacing = 1.2f;
    [SerializeField] private Transform m_slotsParent;

    [Header("Prefabs")]
    [SerializeField] private GameObject m_slotBackgroundPrefab;

    public event Action OnSlotsFull;
    public event Action<int> OnItemsMatched;

    private List<Item> m_items = new List<Item>();
    private List<Transform> m_slotPositions = new List<Transform>();

    public bool IsFull => m_items.Count >= m_maxSlots;
    public int ItemCount => m_items.Count;

    private void Awake()
    {
        CreateSlotPositions();
    }

    private void CreateSlotPositions()
    {
        if (m_slotsParent == null)
        {
            m_slotsParent = this.transform;
        }

        float totalWidth = (m_maxSlots - 1) * m_slotSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < m_maxSlots; i++)
        {
            GameObject slotBg = null;

            if (m_slotBackgroundPrefab != null)
            {
                slotBg = Instantiate(m_slotBackgroundPrefab, m_slotsParent);
            }
            else
            {
                slotBg = new GameObject($"Slot_{i}");
                slotBg.transform.SetParent(m_slotsParent);

                SpriteRenderer sr = slotBg.AddComponent<SpriteRenderer>();

                // Tạo slot background với rounded corners
                Texture2D tex = new Texture2D(100, 100);
                Color[] pixels = new Color[100 * 100];
                Color slotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                Color borderColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

                for (int p = 0; p < pixels.Length; p++)
                {
                    int x = p % 100;
                    int y = p / 100;

                    // Border 3 pixels
                    if (x < 3 || x > 97 || y < 3 || y > 97)
                        pixels[p] = borderColor;
                    else
                        pixels[p] = slotColor;
                }
                tex.SetPixels(pixels);
                tex.Apply();

                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f), 100);
                sr.sprite = sprite;
                sr.sortingOrder = -10;
            }

            Vector3 pos = new Vector3(startX + i * m_slotSpacing, 0, 0);
            slotBg.transform.localPosition = pos;

            m_slotPositions.Add(slotBg.transform);
        }
    }

    public void AddItem(Item item, Action onComplete = null)
    {
        if (IsFull)
        {
            Debug.LogWarning("Slots full!");
            OnSlotsFull?.Invoke();
            return;
        }

        if (item == null || item.View == null)
        {
            Debug.LogError("Item or Item.View is null!");
            onComplete?.Invoke();
            return;
        }

        m_items.Add(item);

        int index = m_items.Count - 1;
        Transform targetPos = m_slotPositions[index];

        Debug.Log($"[SLOTS] Added item, now {m_items.Count}/{m_maxSlots} slots used");

        // Animation bay xuống slot
        item.View.DOMove(targetPos.position, 0.5f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                // Scale lớn hơn một chút trong slot
                item.View.DOScale(Vector3.one * 0.9f, 0.2f);

                // IMPORTANT: Check lose condition (slots full) FIRST before calling onComplete
                // This ensures lose condition is detected BEFORE any win condition check
                bool wasFullBeforeMatch = IsFull;
                Debug.Log($"[SLOTS] Animation complete. wasFullBeforeMatch={wasFullBeforeMatch}, count={m_items.Count}");

                CheckForMatches();

                Debug.Log($"[SLOTS] After CheckForMatches. IsFull={IsFull}, count={m_items.Count}");

                // If slots were full before checking matches, trigger lose FIRST
                if (wasFullBeforeMatch)
                {
                    Debug.Log("[SLOTS] ❌ Slots are FULL - triggering OnSlotsFull event!");
                    OnSlotsFull?.Invoke();
                }

                // Always call onComplete to cleanup (free cell, remove from board)
                // The LayeredBoardController won't check win if slots are full
                Debug.Log("[SLOTS] Calling onComplete callback");
                onComplete?.Invoke();
            });

        // Sorting order cao để luôn hiển thị trên cùng
        SpriteRenderer itemSpr = item.View.GetComponent<SpriteRenderer>();
        if (itemSpr != null)
        {
            itemSpr.sortingOrder = 1000 + index;
        }
    }

    private void CheckForMatches()
    {
        if (m_items.Count < 3) return;

        Dictionary<string, List<Item>> itemGroups = new Dictionary<string, List<Item>>();

        foreach (var item in m_items)
        {
            if (item is NormalItem normalItem)
            {
                string typeKey = normalItem.ItemType.ToString();

                if (!itemGroups.ContainsKey(typeKey))
                {
                    itemGroups[typeKey] = new List<Item>();
                }

                itemGroups[typeKey].Add(item);
            }
        }

        foreach (var group in itemGroups)
        {
            if (group.Value.Count >= 3)
            {
                List<Item> itemsToRemove = group.Value.Take(3).ToList();
                RemoveMatchedItems(itemsToRemove);
                break;
            }
        }
    }

    private void RemoveMatchedItems(List<Item> itemsToRemove)
    {
        foreach (var item in itemsToRemove)
        {
            item.ExplodeView();
            m_items.Remove(item);
        }

        OnItemsMatched?.Invoke(itemsToRemove.Count);
        ReorganizeItems();
    }

    private void ReorganizeItems()
    {
        for (int i = 0; i < m_items.Count; i++)
        {
            Item item = m_items[i];
            Transform targetPos = m_slotPositions[i];

            if (item.View != null)
            {
                item.View.DOMove(targetPos.position, 0.3f).SetEase(Ease.OutQuad);

                SpriteRenderer itemSpr = item.View.GetComponent<SpriteRenderer>();
                if (itemSpr != null)
                {
                    itemSpr.sortingOrder = 1000 + i;
                }
            }
        }
    }

    public void Clear()
    {
        foreach (var item in m_items)
        {
            if (item != null)
            {
                item.Clear();
            }
        }

        m_items.Clear();
    }

    public bool CanMatch()
    {
        if (m_items.Count < 3) return true;

        Dictionary<string, int> typeCounts = new Dictionary<string, int>();

        foreach (var item in m_items)
        {
            if (item is NormalItem normalItem)
            {
                string typeKey = normalItem.ItemType.ToString();

                if (!typeCounts.ContainsKey(typeKey))
                {
                    typeCounts[typeKey] = 0;
                }

                typeCounts[typeKey]++;
            }
        }

        return typeCounts.Values.Any(count => count >= 3);
    }
}