using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainPanel : BasePanel
{
    public override void HideMe()
    {
        
    }

    public override void ShowMe()
    {
        
    }

    protected override void ClickBtn(string btnName)
    {
        switch (btnName)
        {
            case "btnBegin":
                UIMgr.Instance.HidePanel<MainPanel>();
                UIMgr.Instance.ShowPanel<ChooseArchivePanel>();
                break;
            case "btnSetting":
                UIMgr.Instance.ShowPanel<SettingPanel>();
                UIMgr.Instance.HidePanel<MainPanel>();
                break;
            case "btnQuit":
                Application.Quit();
                break;
        }
    }
}
