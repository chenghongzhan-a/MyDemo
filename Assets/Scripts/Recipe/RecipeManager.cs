using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RecipeManager : BaseManager<RecipeManager>
{
    // 缓存：已经加载过的 RecipeListSO
    private Dictionary<int, List<RecipeCreate>> cache = new Dictionary<int, List<RecipeCreate>>();
    // 站台ID → AB包中资源名
    private Dictionary<int, string> stationToAsset = new Dictionary<int, string>
    {
        { 0, "RecipeList_Handcraft" },
    };

    private RecipeManager() { }

    /// <summary>
    /// 获取指定站台的配方列表
    /// 如果已缓存，回调立刻触发；否则异步加载
    /// </summary>
    public void GetRecipesForStation(int stationType, UnityAction<List<RecipeCreate>> callBack)
    {
        //缓存有就直接返回
        if (cache.TryGetValue(stationType, out var cached))
        {
            callBack(cached);
            return;
        }

        //缓存没有则异步加载
        string assetName = stationToAsset[stationType];
        ABResMgr.Instance.LoadResAsync<RecipeList>("Recipe/RecipeList", assetName, (list) =>
        {
            cache[stationType] = list.recipes;
            callBack(list.recipes);
        });
    }

}
