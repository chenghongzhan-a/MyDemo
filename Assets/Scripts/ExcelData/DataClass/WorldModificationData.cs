using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 世界修改数据存储类
/// </summary>
[Serializable]
public class WorldModificationData
{
    /// <summary>
    /// 瓦片修改存储
    /// </summary>
    public Dictionary<string, TileType> tileOverrides = new Dictionary<string, TileType>();
    /// <summary>
    /// 装饰物移除 例如树木 石头
    /// </summary>
    public Dictionary<string, ObjectType> removedObjects = new Dictionary<string, ObjectType>();
    /// <summary>
    /// 玩家手动放置物品的记录
    /// </summary>
    public Dictionary<string, PlacedObjectInfo> placedObjects = new Dictionary<string, PlacedObjectInfo>();
    /// <summary>
    /// 存储地图没有到地的物品
    /// </summary>
    public Dictionary<string, List<ItemSaveData>> objects = new Dictionary<string, List<ItemSaveData>>();
}
