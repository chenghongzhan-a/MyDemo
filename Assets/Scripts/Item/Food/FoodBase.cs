using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodBase : ItemBase
{
    public float hungry;
    public float thirst;
    PlayerArchiveInfo player;
    private void Awake()
    {
        player = ArchiveManager.Instance.currentArchive;
    }
    public override void OnLeftClick()
    {
        base.OnLeftClick();
    }

    public override void OnRightClick()
    {
        player.hunger += hungry;
        if (player.hunger >= player.maxHunger)
        {
            player.hunger = player.maxHunger;
        }
        player.thirst += thirst;
        if (player.thirst >= player.maxThirst)
        {
            player.thirst = player.maxThirst;
        }
    }
}
