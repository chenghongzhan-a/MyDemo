using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 需要的物品数量和品种数据结构
/// </summary>
[System.Serializable]
public struct ItemRequirement
{
    public string itemName;
    public Sprite sprite;
    public int count;
}

[CreateAssetMenu(fileName = "Recipe_", menuName = "Crafting/Recipe")]
public class RecipeCreate : ScriptableObject
{
    /// <summary>
    /// 需要的物品 即输入
    /// </summary>
    public ItemRequirement[] inputs;         
    /// <summary>
    /// 产出的物品 即输出
    /// </summary>
    public ItemRequirement output;        
    /// <summary>
    /// 需要使用的的工作台的编号
    /// </summary>
    public int requiredStationType;
}
