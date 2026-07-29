using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CreateArchivePanel : BasePanel
{
    public TMP_InputField inputField;

    public override void HideMe()
    {
        
    }

    public override void ShowMe()
    {
        inputField.text = "";
    }

    protected override void ClickBtn(string btnName)
    {
        switch (btnName)
        {
            case "btnSure":
                //保存存档
                if (inputField.text != "")
                {
                    //存储数据 并更新面板数据
                    ArchiveManager.Instance.CreateNewArchive(inputField.text);
                }
                //关闭创建存档界面
                UIMgr.Instance.HidePanel<CreateArchivePanel>();
                //刷新选择存档面板的存档数据
                UIMgr.Instance.GetPanel<ChooseArchivePanel>((panel) =>
                {
                    panel.ReFreshArchive();
                });
                break;
            case "btnBack":
                //关闭创建存档界面
                UIMgr.Instance.HidePanel<CreateArchivePanel>();
                break;
        }
    }
}
