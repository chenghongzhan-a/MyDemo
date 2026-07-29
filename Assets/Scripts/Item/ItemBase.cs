using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum E_ItemType
{
    Material,
    Food,
    Drink,
    Axe,
    Draft,
    Shovel,
    Hoe,
    Sword,
    Bow,
    Gun,
    Helmet,
    Armor,
    Leg,
    Shoe,
    Shield,
    Seed,
}
/// <summary>
/// 用于存储的数据
/// </summary>
[Serializable]
public struct ItemSaveData
{
    public string itemName;
    public int count;
    public float posX;
    public float posY;
}

public class ItemBase : MonoBehaviour
{
    /// <summary>
    /// 物品类型
    /// </summary>
    public E_ItemType type;
    /// <summary>
    /// 名字
    /// </summary>
    public string itemName;
    /// <summary>
    /// 是否可以堆叠
    /// </summary>
    public bool isStack;
    /// <summary>
    /// 物品图片
    /// </summary>
    public Sprite icon;
    /// <summary>
    /// 数量
    /// </summary>
    public int count;
    /// <summary>
    /// 是否是在世界中
    /// </summary>
    public bool isWorldItem = false;

    private void OnDestroy()
    {
        //UnregisterFromWorld();
    }

    public virtual void OnLeftClick()
    {

    }

    public virtual void OnRightClick()
    {

    }

    /// <summary>
    /// 注册到世界中使用的方法（掉落物落地后调用）
    /// </summary>
    public void RegisterToWorld()
    {
        if (!isWorldItem) return;
        if (ArchiveManager.Instance == null) return;
        if (ArchiveManager.Instance.currentWorldMod == null) return;

        Vector3 pos = transform.position;
        Vector2Int chunkCoord = MapGenerator.WorldToChunk(pos, MapGenerator.ChunkSize);
        Vector2Int localPos   = MapGenerator.WorldToLocal(pos, MapGenerator.ChunkSize);
        ArchiveManager.Instance.SaveGameObject(chunkCoord, localPos, GetSaveData());
    }

    /// <summary>
    /// 从世界中移除时使用的方法（掉落物被捡起时调用）
    /// </summary>
    public void UnregisterFromWorld()
    {
        if (!isWorldItem) return;
        if (ArchiveManager.Instance == null) return;
        if (ArchiveManager.Instance.currentWorldMod == null) return;

        Vector3 pos = transform.position;
        Vector2Int chunkCoord = MapGenerator.WorldToChunk(pos, MapGenerator.ChunkSize);
        Vector2Int localPos   = MapGenerator.WorldToLocal(pos, MapGenerator.ChunkSize);
        ArchiveManager.Instance.RemoveGameObject(chunkCoord, localPos, GetSaveData());
    }

    /// <summary>存档前调用：把自己打包成可序列化的纯数据</summary>
    public ItemSaveData GetSaveData()
    {
        return new ItemSaveData
        {
            itemName = this.itemName,
            count = this.count,
            posX = transform.position.x,
            posY = transform.position.y,
        };
    }

    /// <summary>读档后调用：把数据贴回到新实例化的物品上</summary>
    public void LoadFromSaveData(ItemSaveData data)
    {
        this.count = data.count;
        // position 由 Instantiate 时设置，不用管
    }
}

/// <summary>
/// 物品栏槽位的存档数据（纯数据，不持有 GameObject 引用）
/// </summary>
[Serializable]
public class InventorySlotData
{
    public int slotIndex;
    public string itemName;
    public int count;
}
