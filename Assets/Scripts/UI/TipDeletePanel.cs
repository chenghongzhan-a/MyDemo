using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TipDeletePanel : BasePanel
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
            case "btnSure":
                //????
                UIMgr.Instance.GetPanel<ChooseArchivePanel>((panel) =>
                {
                    if (panel.nowChooseArchiveName != null)
                    {
                        ArchiveManager.Instance.DeleteArchive(panel.nowChooseArchiveName);
                        //????
                        panel.ReFreshArchive();
                    }
                });
                UIMgr.Instance.HidePanel<TipDeletePanel>();
                break;
            case "btnBack":
                UIMgr.Instance.HidePanel<TipDeletePanel>();
                UIMgr.Instance.ShowPanel<ChooseArchivePanel>();
                break;
        }
    }
}
