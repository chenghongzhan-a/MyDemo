using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeObj : BaseDecoration
{
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
    }
}
