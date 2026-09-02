using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawnManager : BaseManager<MonsterSpawnManager>
{
    /// <summary>
    /// 怪物生成半径
    /// </summary>
    public float spawnRadius = 10;
    /// <summary>
    /// 怪物生成CD
    /// </summary>
    public float spawnCoolDown = 2;
    /// <summary>
    /// 怪物生成计时
    /// </summary>
    private float timer;
    /// <summary>
    /// 怪物生成数量
    /// </summary>
    public float maxMonsterNum = 3;
    //目前怪物数量
    public int monsterCount;
    //玩家位置
    private Transform player;

    private MonsterSpawnManager()
    {
        player = GameObject.Find("Player").transform;
        MonoMgr.Instance.AddUpdateListener(SpawnMonster);
    }

    public void SpawnMonster()
    {
        timer += Time.deltaTime;
        if (timer < spawnCoolDown)
        {
            return;
        }
        timer = 0;

        if (player == null)
        {
            return;
        }

        monsterCount = GameObject.FindGameObjectsWithTag("Monster").Length;
        if (monsterCount >= maxMonsterNum)
        {
            return;
        }
        //得到玩家位置
        Vector2 center = player.transform.position;
        //获取怪物刷新位置
        Vector2 candidate = center + Random.insideUnitCircle * spawnRadius;

        //离玩家太近不生成
        if (Vector2.Distance(center, candidate) < 5f)
        {
            return;
        }
        //瓦片类型不对也不生成
        TileType tile = MapGenerator.Instance.GetTileAtWorld(candidate);
        if (tile == TileType.deepSea || tile == TileType.pond || tile == TileType.shallowSea)
        {
            return;
        }
        //异步加载怪物预设体
        ABResMgr.Instance.LoadResAsync<GameObject>("biology", "WildPig", (prefab) =>
        {
            if (prefab == null) return;
            GameObject pig = Object.Instantiate(prefab, candidate, Quaternion.identity);
        });
        monsterCount++;
    }
}
