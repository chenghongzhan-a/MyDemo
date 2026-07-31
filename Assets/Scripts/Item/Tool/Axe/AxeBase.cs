using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxeBase : ItemBase
{
/// <summary>
    /// 伤害
    /// </summary>
    public int damage;
    /// <summary>
    /// 使用间隔
    /// </summary>
    public int useTime;

    private Collider2D axeCollider;
    private Animator animator;

    void Awake()
    {
        axeCollider = GetComponentInChildren<Collider2D>();
        axeCollider.enabled = false;
        animator = GetComponent<Animator>();
    }

    public override void OnLeftClick()
    {
        animator.SetTrigger("OnLeftDown");
    }

    public virtual void AxeAtk()
    {    
        axeCollider.enabled = true;
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        Collider2D[] hits = new Collider2D[10];
        int count = axeCollider.OverlapCollider(filter, hits);
    
        for (int i = 0; i < count; i++)
        {
            hits[i].GetComponent<TreeObj>()?.TakeDamage(damage);
        }
        
    }

    public virtual void AxeAtkEnd()
    {
        axeCollider.enabled = false;
    }
}
