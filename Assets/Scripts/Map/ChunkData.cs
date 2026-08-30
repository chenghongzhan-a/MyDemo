using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 瓦片类型枚举
/// </summary>
public enum TileType
{
    deepSea,        // 深海（不可通行）
    shallowSea,     // 浅海
    sand,           // 沙滩
    grass,          // 草地
    forest,         // 森林（密集植被）
    swamp,          // 沼泽
    pond,           // 池塘
    dirt,           // 泥土
    snow,           // 雪地
}

/// <summary>
/// 装饰物类型枚举
/// </summary>
public enum ObjectType
{
    none,           // 无
    tree,           // 树木
    rock,           // 石头
    bush,           // 灌木
    flower,         // 花朵
}

/// <summary>
/// 单个瓦片的数据
/// </summary>
[System.Serializable]
public struct TileData
{
    /// <summary>
    /// 瓦片的地形类型
    /// </summary>
    public TileType type;

    /// <summary>
    /// 是否可通行
    /// </summary>
    public bool walkable;

    /// <summary>
    /// 此瓦片上的装饰物类型
    /// </summary>
    public ObjectType objectOnTile;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TileData(TileType type)
    {
        this.type = type;
        this.objectOnTile = ObjectType.none;
        //根据地形自动设置通行性
        this.walkable = type != TileType.deepSea;
    }
}

/// <summary>
/// 区块数据
/// 存储一个区块的完整信息：位置、尺寸、瓦片二维数组、状态等。
/// </summary>
[System.Serializable]
public class ChunkData
{
    /// <summary>
    /// 区块坐标
    /// </summary>
    public Vector2Int chunkCoord;

    /// <summary>
    /// 区块左下角的世界坐标
    /// </summary>
    public Vector2Int worldPos;

    /// <summary>
    /// 区块边长
    /// </summary>
    public int chunkSize;

    /// <summary>
    /// 二维瓦片数组
    /// </summary>
    public TileData[,] tiles;
    /// <summary>
    /// 区块内的装饰物列表。
    /// 存储每个装饰物的本地坐标和类型，用于生成GameObject或Sprite。
    /// </summary>
    public List<ObjectSpawnInfo> objects;

    /// <summary>
    /// 此区块数据是否已经过噪声计算
    /// </summary>
    public bool isGenerated;

    /// <summary>
    /// 此区块的GameObject是否已在场景中实例化
    /// </summary>
    public bool isActive;

    /// <summary>
    /// 记录此区块的GameObjects 用于卸载时批量销毁
    /// </summary>
    [System.NonSerialized]
    public List<GameObject> spawnedObjects;

    /// <summary>
    /// 创建一个新的区块数据实例
    /// </summary>
    /// <param name="coord">区块坐标</param>
    /// <param name="size">区块边长</param>
    public ChunkData(Vector2Int coord, int size)
    {
        chunkCoord = coord;
        chunkSize = size;
        worldPos = new Vector2Int(coord.x * size, coord.y * size);
        tiles = new TileData[size, size];
        objects = new List<ObjectSpawnInfo>();
        spawnedObjects = new List<GameObject>();
        isActive = false;
        isGenerated = false;
    }

    /// <summary>
    /// 获取指定本地坐标的瓦片数据（带边界检查）
    /// </summary>
    public TileData GetTile(int localX, int localY)
    {
        if (localX < 0 || localX >= chunkSize || localY < 0 || localY >= chunkSize)
        {
            Debug.LogWarning($"GetTile 越界: ({localX}, {localY}), 区块大小={chunkSize}");
            return new TileData(TileType.deepSea);
        }
        return tiles[localX, localY];
    }

    /// <summary>
    /// 设置指定本地坐标的瓦片数据（带边界检查）
    /// </summary>
    public void SetTile(int localX, int localY, TileData data)
    {
        if (localX >= 0 && localX < chunkSize && localY >= 0 && localY < chunkSize)
        {
            tiles[localX, localY] = data;
        }
    }
}

/// <summary>
/// 装饰物生成信息
/// </summary>
[System.Serializable]
public struct ObjectSpawnInfo
{
    /// <summary>装饰物在区块内的本地坐标</summary>
    public Vector2Int localPos;

    /// <summary>装饰物类型</summary>
    public ObjectType type;

    public ObjectSpawnInfo(int x, int y, ObjectType type)
    {
        localPos = new Vector2Int(x, y);
        this.type = type;
    }
}
