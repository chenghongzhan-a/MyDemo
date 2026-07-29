using System;
using System.Collections.Generic;

[Serializable]
public class PlayerArchiveInfo
{
    /// <summary>
    /// 存档id
    /// </summary>
    public int id;
    /// <summary>
    /// 名字
    /// </summary>
    public string name;

    public string createTime;
    public string lastLogTime;

    public int worldSeed;

    /// <summary>
    /// 玩家位置
    /// </summary>
    public float playerPosX;
    public float playerPosY;

    /// <summary>
    /// 玩家属性
    /// </summary>
    public float health;
    public float maxHealth;
    public float hunger;
    public float maxHunger;
    public float thirst;
    public float maxThirst;

    /// <summary>
    /// 物品栏存档数据
    /// </summary>
    public List<InventorySlotData> inventory = new List<InventorySlotData>();
    /// <summary>
    /// 玩家背包存档数据
    /// </summary>
    public List<InventorySlotData> bagInventory = new List<InventorySlotData>();
}