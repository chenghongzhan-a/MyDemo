using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        MusicMgr.Instance.PlayBKMusic("otherside");
        UIMgr.Instance.ShowPanel<SettingPanel>();
        UIMgr.Instance.HidePanel<SettingPanel>();
        UIMgr.Instance.ShowPanel<MainPanel>();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
