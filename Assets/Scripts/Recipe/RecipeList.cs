using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipe_", menuName = "Crafting/RecipeList")]
public class RecipeList : ScriptableObject
{
    public List<RecipeCreate> recipes;
}
