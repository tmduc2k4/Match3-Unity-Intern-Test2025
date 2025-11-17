using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TileData
{
    public int x;
    public int y;
    public int layer;
    public NormalItem.eNormalType itemType;

    public TileData(int x, int y, int layer, NormalItem.eNormalType itemType)
    {
        this.x = x;
        this.y = y;
        this.layer = layer;
        this.itemType = itemType;
    }
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int levelNumber;
    public string levelName;

    [Header("Board Settings")]
    public int maxLayers = 1;
    public List<TileData> Tiles = new List<TileData>();

    [Header("Win Conditions")]
    public int targetScore = 100;
    public int moveLimit = -1;
    public float timeLimit = -1f;

    public static LevelData CreateRandomLevel(int levelNumber, int numLayers = 1, int tilesPerLayer = 18)
    {
        LevelData level = ScriptableObject.CreateInstance<LevelData>();
        level.levelNumber = levelNumber;
        level.levelName = $"Level {levelNumber}";
        level.maxLayers = numLayers;
        level.Tiles = new List<TileData>();

        System.Random rnd = new System.Random(levelNumber);

        // Đảm bảo số tiles chia hết cho 3
        int totalTiles = tilesPerLayer;
        while (totalTiles % 3 != 0)
        {
            totalTiles++;
        }

        // Tạo danh sách item types (mỗi loại xuất hiện 3 lần)
        List<NormalItem.eNormalType> itemTypes = new List<NormalItem.eNormalType>();
        int typesNeeded = totalTiles / 3;

        for (int i = 0; i < typesNeeded; i++)
        {
            NormalItem.eNormalType type = (NormalItem.eNormalType)(i % System.Enum.GetValues(typeof(NormalItem.eNormalType)).Length);
            itemTypes.Add(type);
            itemTypes.Add(type);
            itemTypes.Add(type);
        }

        // Shuffle items
        for (int i = itemTypes.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            var temp = itemTypes[i];
            itemTypes[i] = itemTypes[j];
            itemTypes[j] = temp;
        }

        // Tạo layout xếp chồng (như trong hình)
        level.Tiles = CreateStackedLayout(totalTiles, itemTypes, rnd);

        Debug.Log($"[LEVEL] Created stacked level {levelNumber} with {level.Tiles.Count} tiles");
        return level;
    }

    private static List<TileData> CreateStackedLayout(int totalTiles, List<NormalItem.eNormalType> itemTypes, System.Random rnd)
    {
        List<TileData> tiles = new List<TileData>();
        int tileIndex = 0;

        // Tạo layout theo hình dạng giống hình ảnh
        // Base layer: 5x3 grid (dưới cùng - nhiều tiles nhất)
        for (int y = 0; y < 3 && tileIndex < totalTiles; y++)
        {
            for (int x = -2; x <= 2 && tileIndex < totalTiles; x++)
            {
                tiles.Add(new TileData(x, y, 0, itemTypes[tileIndex]));
                tileIndex++;
            }
        }

        // Middle layer: 3x2 grid (giữa - overlap lên base layer)
        for (int y = 1; y < 3 && tileIndex < totalTiles; y++)
        {
            for (int x = -1; x <= 1 && tileIndex < totalTiles; x++)
            {
                tiles.Add(new TileData(x, y, 0, itemTypes[tileIndex]));
                tileIndex++;
            }
        }

        // Top layer: random positions (trên cùng - ít tiles)
        int remainingTiles = totalTiles - tileIndex;
        for (int i = 0; i < remainingTiles && tileIndex < totalTiles; i++)
        {
            int x = rnd.Next(-1, 2);
            int y = rnd.Next(2, 4);

            tiles.Add(new TileData(x, y, 0, itemTypes[tileIndex]));
            tileIndex++;
        }

        return tiles;
    }

    public bool IsValid()
    {
        if (Tiles == null || Tiles.Count == 0) return false;
        if (maxLayers <= 0) return false;
        if (Tiles.Count % 3 != 0)
        {
            Debug.LogWarning($"Level {levelNumber}: Tile count ({Tiles.Count}) not divisible by 3!");
            return false;
        }

        return true;
    }
}