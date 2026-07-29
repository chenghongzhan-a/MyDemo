using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlacedObjectInfo
{
    //预设体名字
    public string prefabName;

    //世界坐标
    public float worldX;
    public float worldY;
    public int growStage;
    public float nowGorwTime;
}
