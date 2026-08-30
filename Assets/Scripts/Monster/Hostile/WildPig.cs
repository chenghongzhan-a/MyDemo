using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WildPig : MonsterBase
{
    public void Atk()
    {
        if (isDead || player == null) return;

        //动画播到伤害帧时玩家可能已经跑出范围，需要再次判定
        if (DistanceToPlayer() <= atkRange)
        {
            //通过事件中心通知玩家扣血
            EventCenter.Instance.EventTrigger(E_EventType.E_MonsterAttack, atk);
        }
        animator.SetBool("isAtk", false);
    }
}
