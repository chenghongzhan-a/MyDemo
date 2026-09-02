using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RecipeManager : BaseManager<RecipeManager>
{
    private Dictionary<int, List<UnityAction<List<RecipeCreate>>>> pending =
    new Dictionary<int, List<UnityAction<List<RecipeCreate>>>>();

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
        if (cache.TryGetValue(stationType, out var cached))
        {
            callBack(cached);
            return;
        }

        if (!pending.ContainsKey(stationType))
        {
            pending[stationType] = new List<UnityAction<List<RecipeCreate>>>();

            ABResMgr.Instance.LoadResAsync<RecipeList>("recipe",
                stationToAsset[stationType], (list) =>
                {
                    cache[stationType] = list.recipes;

                    foreach (var cb in pending[stationType])
                        cb(list.recipes);
                    pending.Remove(stationType);
                });
        }

        pending[stationType].Add(callBack);
    }
}
