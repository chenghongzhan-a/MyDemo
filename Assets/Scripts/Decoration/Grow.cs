using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grow : MonoBehaviour
{
    [Header("生长阶段")]
    public float growTime = 60f;          // 每个阶段需要的时间
    public Sprite[] spriteS;         // 每个阶段的精灵图片，类型为 Sprite[]
    private Sprite sprite;

    [Header("生长状态")]
    
    //当前生长阶段
    public int currentStage = 0;
    //现在已经生长的时间
    public float nowGorwTime = 0f;
    //是否生长完成    
    public bool isFullyGrown = true;

    //生长过程和完成后要做的事
    public Action<int> OnStageChanged;
    public Action OnFullyGrown;
    private void Awake()
    {
        sprite = this.gameObject.GetComponent<SpriteRenderer>().sprite;
    }

    void Update()
    {
        //如果已经是最终阶段 就停止生长
        if (isFullyGrown) return;

        nowGorwTime += Time.deltaTime;
        if (nowGorwTime >= growTime)
        {
            nowGorwTime = 0f;
            currentStage++;
            AdvanceStage();
        }
    }
    //切换显示图片
    private void AdvanceStage() 
    {
        sprite = spriteS[currentStage];
        GetComponent<SpriteRenderer>().sprite = sprite;
        if (currentStage == 3)
        {
            isFullyGrown = true;
        }
    }
    /// <summary>
    /// 将植物种植为幼苗 初始化状态
    /// </summary>
    public void PlantAsSeedling()
    {
        currentStage = 0;
        nowGorwTime = 0f;
        isFullyGrown = false;
        //初始化图片
        AdvanceStage();
    }
    /// <summary>
    /// 获取生长的百分比
    /// </summary>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetGrowthProgress() => (float)currentStage / spriteS.Length;
}
