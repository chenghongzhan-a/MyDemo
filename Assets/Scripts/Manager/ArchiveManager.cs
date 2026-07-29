using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ArchiveManager : BaseManager<ArchiveManager>
{
    // 所有存档文件名称列表（用于显示在UI上）
    private List<string> archiveFileNames = new List<string>();
    FileInfo[] files;

    // 存储玩家相关的信息修改数据
    public PlayerArchiveInfo currentArchive;
    // 存储世界修改数据
    public WorldModificationData currentWorldMod;
    public string currentArchiveKey;
    public int id;

    private ArchiveManager()
    {
        RefreshArchiveList();
    }

    #region 存档管理
    // 刷新硬盘上的存档列表
    public void RefreshArchiveList()
    {
        archiveFileNames.Clear();
        if (!Directory.Exists(BinaryDataMgr.SAVE_PATH))
        {
            Directory.CreateDirectory(BinaryDataMgr.SAVE_PATH);
        }

        DirectoryInfo dir = new DirectoryInfo(BinaryDataMgr.SAVE_PATH);
        files = dir.GetFiles("Archive_*.tang");

        foreach (FileInfo file in files)
        {
            archiveFileNames.Add(Path.GetFileNameWithoutExtension(file.Name));
        }

        Debug.Log($"找到 {archiveFileNames.Count} 个存档文件");
    }

    // 给 UI 层调用，获取存档列表
    public List<string> GetArchiveList()
    {
        return archiveFileNames;
    }


    // 加载指定存档
    public PlayerArchiveInfo LoadArchive(string fileName)
    {
        return BinaryDataMgr.Instance.Load<PlayerArchiveInfo>(fileName);
    }

    /// <summary>
    /// 自动生成一个新的玩家ID
    /// </summary>
    private int GenerateNewPlayerId()
    {
        // 1. 刷新存档列表（确保获取最新数据）
        RefreshArchiveList();

        // 2. 如果没有任何存档，从 1001 开始
        if (archiveFileNames.Count == 0)
        {
            return 1001;
        }

        // 3. 遍历所有存档文件，遍历取ID，找出最大值
        int maxId = 0;
        foreach (string fileName in archiveFileNames)
        {
            // 文件名格式：Archive_1001
            // 去掉 "Archive_" 前缀，剩下的就是ID
            string idStr = fileName.Replace("Archive_", "");
            if (int.TryParse(idStr, out int id))
            {
                if (id > maxId)
                {
                    maxId = id;
                }
            }
        }

        // 4. 返回 最大ID + 1
        return maxId + 1;
    }

    /// <summary>
    /// 创建新存档（自动生成ID）
    /// </summary>
    public void CreateNewArchive(string archiveName)
    {
        // 自动生成唯一ID
        int newId = GenerateNewPlayerId();

        // 创建存档数据
        PlayerArchiveInfo newPlayer = new PlayerArchiveInfo
        {
            id = newId,
            name = archiveName,
            createTime = DateTime.Now.ToString(),
            lastLogTime = DateTime.Now.ToString(),
            worldSeed = DateTime.Now.GetHashCode(),
            playerPosX = 0,
            playerPosY = 0,
            health = 100,
            maxHealth = 100,
            hunger = 100,
            maxHunger = 100,
            thirst = 100,
            maxThirst= 100,
        };

        // 保存到文件
        string fileName = "Archive_" + newId;
        BinaryDataMgr.Instance.Save(newPlayer, fileName);

        // 刷新列表并更新UI
        RefreshArchiveList();

        Debug.Log($"创建新存档成功！ID: {newId}，名称: {archiveName}");
    }

    /// <summary>
    /// 删除指定存档（会删除对应的文件）
    /// </summary>
    /// <param name="fileName">存档文件名（不含扩展名），如 "Archive_1001"</param>
    /// <returns>是否删除成功</returns>
    public bool DeleteArchive(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("文件名称不能为空");
            return false;
        }

        string filePath = Path.Combine(BinaryDataMgr.SAVE_PATH, fileName + ".tang");

        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"存档文件不存在: {filePath}");
                return false;
            }

            // 执行删除
            File.Delete(filePath);
            Debug.Log($"存档已删除: {fileName}");

            // 同步删除对应的世界修改数据文件
            string archiveId = fileName.Replace("Archive_", "");
            string worldModPath = Path.Combine(BinaryDataMgr.SAVE_PATH, $"WorldMod_{archiveId}.tang");
            if (File.Exists(worldModPath))
            {
                File.Delete(worldModPath);
                Debug.Log($"世界数据已同步删除: WorldMod_{archiveId}.tang");
            }

            // 刷新列表并更新UI
            RefreshArchiveList();

            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.LogError($"没有权限删除文件: {e.Message}");
            return false;
        }
        catch (IOException e)
        {
            Debug.LogError($"IO错误（文件可能正在使用）: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"删除失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 更新存档里的数据（但不是保存）
    /// 可以在任何时候调用，建议不要在Update中频繁保存游戏
    /// </summary>
    /// <param name="playerData">要更新的玩家数据</param>
    /// <param name="fileName">存档文件名（不含扩展名），如 "Archive_1001"</param>
    /// <returns>是否保存成功</returns>
    public bool UpdateArchive(PlayerArchiveInfo playerData, string fileName)
    {
        if (playerData == null)
        {
            Debug.LogError("存档数据为空，无法更新");
            return false;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("文件名称不能为空");
            return false;
        }

        try
        {
            // 更新最后登录时间
            playerData.lastLogTime = DateTime.Now.ToString();

            // 保存到文件（覆盖写入）
            BinaryDataMgr.Instance.Save(playerData, fileName);

            Debug.Log($"存档已更新: {fileName}，玩家: {playerData.name}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"更新存档失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 保存当前正在进行的游戏进度
    /// </summary>
    /// <param name="playerData">当前玩家数据</param>
    /// <returns>是否保存成功</returns>
    public bool SaveCurrentGame(PlayerArchiveInfo playerData)
    {
        if (playerData == null)
        {
            Debug.LogError("当前游戏数据为空");
            return false;
        }

        // 根据玩家ID生成文件名
        string fileName = "Archive_" + playerData.id;
        return UpdateArchive(playerData, fileName);
    }
    #endregion

    #region 地图存储相关
    //加载地图时调用
    public void Load(int archiveId)
    {
        string fileName = $"WorldMod_{archiveId}";
        currentWorldMod = BinaryDataMgr.Instance.Load<WorldModificationData>(fileName)
                      ?? new WorldModificationData();
    }

    //存档地图时调用
    public void Save(int archiveId)
    {
        string fileName = $"WorldMod_{archiveId}";
        BinaryDataMgr.Instance.Save(currentWorldMod, fileName);
    }

    /// <summary>
    /// 记录被移除的装饰物 即自然生成的物质
    /// </summary>
    /// <param name="chunkCoord">区块坐标</param>
    /// <param name="localPos">区块中的瓦片本地坐标</param>
    /// <param name="type">装饰物枚举</param>
    public void MarkObjectRemoved(Vector2Int chunkCoord, Vector2Int localPos, ObjectType type)
    {
        string key = MakeKey(chunkCoord, localPos);
        if (!currentWorldMod.removedObjects.ContainsKey(key))
        {
            currentWorldMod.removedObjects.Add(key, type);
        }
    }


    /// <summary>查询某个位置的装饰物是否已被移除（地图生成时使用）</summary>
    public bool IsObjectRemoved(Vector2Int chunkCoord, Vector2Int localPos)
    {
        return currentWorldMod.removedObjects.ContainsKey(MakeKey(chunkCoord, localPos));
    }

    /// <summary>记录瓦片被修改（挖地、铺路等）</summary>
    public void MarkTileChanged(Vector2Int chunkCoord, Vector2Int localPos, TileType newType)
    {
        string key = MakeKey(chunkCoord, localPos);
        currentWorldMod.tileOverrides[key] = newType;
    }

    /// <summary>查询瓦片是否被修改过（地图加载时使用）</summary>
    public bool TryGetTileOverride(Vector2Int chunkCoord, Vector2Int localPos, out TileType type)
    {
        return currentWorldMod.tileOverrides.TryGetValue(MakeKey(chunkCoord, localPos), out type);
    }

    /// <summary>
    /// 更新生长状态
    /// </summary>
    /// <param name="chunkCoord">区块坐标</param>
    /// <param name="localPos">区块内瓦片坐标</param>
    /// <param name="growStage">生长阶段</param>
    /// <param name="growTime">已经生长时间</param>
    /// <param name="isFullyGrown">是否生长完成</param>
    public void UpdatePlacedObjectInfo(Vector2Int chunkCoord, Vector2Int localPos, int growStage, float growTime, bool isFullyGrown)
    {
        string key = MakeKey(chunkCoord, localPos);

        if (currentWorldMod.placedObjects.TryGetValue(key, out PlacedObjectInfo info))
        {
            //找到字典项，更新生长相关字段
            info.growStage = growStage;
            info.nowGorwTime = growTime;
        }
        else
        {
            // 如果字典里没有
            Debug.LogWarning($"试图更新不存在的物体: {key}");
        }
    }

    /// <summary>
    /// 记录玩家放置的物品
    /// </summary>
    /// <param name="chunkCoord">区块坐标</param>
    /// <param name="localPos">区块中的瓦片本地坐标</param>
    /// <param name="type">玩家放置的物品的信息</param>
    public void MarkObjectPlaced(Vector2Int chunkCoord, Vector2Int localPos, PlacedObjectInfo type)
    {
        string key = MakeKey(chunkCoord, localPos);
        if (!currentWorldMod.placedObjects.ContainsKey(key))
        {
            currentWorldMod.placedObjects.Add(key, type);
        }
    }

    /// <summary>
    /// 移除存入的玩家放置物品
    /// </summary>
    /// <param name="chunkCoord"></param>
    /// <param name="localPos"></param>
    public void MarkObjectUnplaced(Vector2Int chunkCoord, Vector2Int localPos)
    {
        currentWorldMod.placedObjects.Remove(MakeKey(chunkCoord, localPos));
    }
    #endregion

    #region 地图掉落物品存储
    /// <summary>
    /// 存储地面上的掉落物，地图保存时使用，在加载地图时重新生成出来
    /// </summary>
    public void SaveGameObject(Vector2Int chunkCoord, Vector2Int localPos, ItemSaveData data)
    {
        string key = MakeKey(chunkCoord, localPos);
        if (!currentWorldMod.objects.ContainsKey(key))
            currentWorldMod.objects[key] = new List<ItemSaveData>();
        currentWorldMod.objects[key].Add(data);
    }

    /// <summary>
    /// 移除存储的掉落物，在物品被捡起时调用
    /// </summary>
    public void RemoveGameObject(Vector2Int chunkCoord, Vector2Int localPos, ItemSaveData data)
    {
        string key = MakeKey(chunkCoord, localPos);
        if (currentWorldMod.objects.TryGetValue(key, out var list))
        {
            list.Remove(data);
            if (list.Count == 0)
                currentWorldMod.objects.Remove(key);
        }
    }
    #endregion

    /// <summary>
    /// 查询指定位置是否有存档的掉落物（地图加载区块时使用）
    /// </summary>
    public bool TryGetDroppedItems(Vector2Int chunkCoord, Vector2Int localPos, out List<ItemSaveData> items)
    {
        return currentWorldMod.objects.TryGetValue(MakeKey(chunkCoord, localPos), out items);
    }

    /// <summary>
    /// 获取存储键
    /// </summary>
    private static string MakeKey(Vector2Int chunkCoord, Vector2Int localPos)
    {
        return $"{chunkCoord.x}_{chunkCoord.y}_{localPos.x}_{localPos.y}";
    }
}
