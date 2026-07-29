using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SeedBase : ItemBase
{
    public override void OnLeftClick()
    {
        
    }

    public override void OnRightClick()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mouseWorldPos = MapGenerator.Instance.playerCamera.ScreenToWorldPoint(Input.mousePosition);

        // ===== 加这段：检查这个瓦片是否已经有东西了 =====
        Vector2Int chunkCoord = MapGenerator.Instance.WorldToChunk(mouseWorldPos);
        Vector2Int localPos = MapGenerator.Instance.WorldToLocal(mouseWorldPos);
        string key = $"{chunkCoord.x}_{chunkCoord.y}_{localPos.x}_{localPos.y}";
        if (ArchiveManager.Instance.currentWorldMod.placedObjects.ContainsKey(key))
            return;  // 已经有玩家放置的物品，不能再种
                     // ===== 检查结束 =====

        switch (MapGenerator.Instance.GetTileAtWorld(mouseWorldPos))
        {
            case TileType.grass:
            case TileType.forest:
            case TileType.dirt:
                GameObject tree = MapGenerator.Instance.SpawnSingleObject(ObjectType.tree, mouseWorldPos);
                tree.GetComponent<Grow>()?.PlantAsSeedling();
                tree.GetComponent<SpriteRenderer>().sortingLayerName = "Objects";
                tree.GetComponent<SpriteRenderer>().sortingOrder = -(int)(tree.transform.position.y * 100);
                UIMgr.Instance.GetPanel<GamePanel>((panel) =>
                {
                    panel.ConsumeEquippedItem();
                    panel.SaveInventoryToArchive();
                    ArchiveManager.Instance.SaveCurrentGame(ArchiveManager.Instance.currentArchive);
                });
                break;
        }
    }
}
