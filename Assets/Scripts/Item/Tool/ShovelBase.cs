using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShovelBase : ItemBase
{
    /// <summary>
    /// 伤害
    /// </summary>
    public int damage;
    /// <summary>
    /// 使用间隔
    /// </summary>
    public int useTime;

    public override void OnLeftClick()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            // 点击的是UI，不执行开枪
            return;
        }
        else
        {
            //获取鼠标点击对应坐标的瓦片类型
            switch (MapGenerator.Instance.GetTileAtWorld(MapGenerator.Instance.playerCamera.ScreenToWorldPoint(Input.mousePosition)))
            {
                case TileType.deepSea:
                    break;
                case TileType.shallowSea:
                    break;
                case TileType.sand:
                    MapGenerator.Instance.SetTileAtWorld(MapGenerator.Instance.playerCamera.ScreenToWorldPoint(Input.mousePosition), TileType.dirt);
                    break;
                case TileType.grass:
                    MapGenerator.Instance.SetTileAtWorld(MapGenerator.Instance.playerCamera.ScreenToWorldPoint(Input.mousePosition), TileType.dirt);
                    break;
                case TileType.forest:
                    break;
                case TileType.swamp:
                    break;
                case TileType.pond:
                    break;
            }
        }
    }
}
