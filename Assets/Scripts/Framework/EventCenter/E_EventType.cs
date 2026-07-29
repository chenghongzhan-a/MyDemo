using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件类型 枚举
/// </summary>
[SerializeField]
public enum E_EventType 
{
    /// <summary>
    /// 怪物死亡事件 —— 参数：Monster
    /// </summary>
    E_Monster_Dead,
    /// <summary>
    /// 玩家获取奖励 —— 参数：int
    /// </summary>
    E_Player_GetReward,
    /// <summary>
    /// 测试用事件 —— 参数：无
    /// </summary>
    E_Test,
    /// <summary>
    /// 场景切换时进度变化获取
    /// </summary>
    E_SceneLoadChange,

    /// <summary>
    /// 使用物品
    /// </summary>
    E_Left,

    /// <summary>
    /// 放置物品
    /// </summary>
    E_Right,

    /// <summary>
    /// 水平热键 -1~1的事件监听
    /// </summary>
    E_Input_Horizontal,

    /// <summary>
    /// 竖直热键 -1~1的事件监听
    /// </summary>
    E_Input_Vertical,

    /// <summary>
    /// 怪物攻击
    /// </summary>
    E_MonsterAttack,
}
