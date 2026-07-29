using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家放置的物品标记类
/// </summary>
public class PlayerPlacedObject : MonoBehaviour
{
    public PlacedObjectInfo p;

    public PlayerPlacedObject() { }

    /// <summary>
    /// 刷新存档中的信息
    /// </summary>
    public void RefreshArchive()
    {
        Grow grow = GetComponent<Grow>();
        if (ArchiveManager.Instance == null) return;

        int stage = grow != null ? grow.currentStage : 0;
        float time = grow != null ? grow.nowGorwTime : 0f;
        bool grown = grow != null && grow.isFullyGrown;

        ArchiveManager.Instance.UpdatePlacedObjectInfo(MapGenerator.Instance.WorldToChunk(this.transform.position),
                                                       MapGenerator.Instance.WorldToLocal(this.transform.position), 
                                                       stage, time, grown);
    }
}
