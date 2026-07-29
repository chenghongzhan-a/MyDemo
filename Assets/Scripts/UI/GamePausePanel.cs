using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePausePanel : BasePanel
{
    Transform player;

    public override void HideMe()
    {

    }

    public override void ShowMe()
    {
        player = GameObject.Find("Player").transform;


        SliderValueChange("sliderMusic", PlayerPrefs.GetFloat("bkMusicValue"));
        SliderValueChange("sliderSound", PlayerPrefs.GetFloat("soundValue"));

        ToggleValueChange("togMusic", PlayerPrefs.GetInt("BKMusic") == 1 ? true : false);
        ToggleValueChange("togSound", PlayerPrefs.GetInt("Sound") == 1 ? true : false);
        //��ͣ��Ϸ
    }

    protected override void ClickBtn(string btnName)
    {
        switch (btnName)
        {
            case "btnClose":
                UIMgr.Instance.HidePanel<GamePausePanel>();
                break;
            case "btnSave":
                //保存玩家数据
                ArchiveManager.Instance.currentArchive.playerPosX = player.position.x;
                ArchiveManager.Instance.currentArchive.playerPosY = player.position.y;
                //保存物品栏数据后再写档
                UIMgr.Instance.GetPanel<GamePanel>((panel) =>
                {
                    panel.SaveInventoryToArchive();
                    ArchiveManager.Instance.SaveCurrentGame(ArchiveManager.Instance.currentArchive);
                });
                //保存地图数据
                //刷新内存中的放置物数据
                PlayerPlacedObject[] allPlacedObjects = FindObjectsByType<PlayerPlacedObject>(FindObjectsSortMode.None);
                foreach (var obj in allPlacedObjects)
                {
                    obj.RefreshArchive(); //更新内存里的字典
                }

                //保存到硬盘
                ArchiveManager.Instance.Save(ArchiveManager.Instance.id);

                Debug.Log($" 游戏已保存！共更新 {allPlacedObjects.Length} 个物体");
                break;
            case "btnBack":
                UIMgr.Instance.HidePanel<GamePausePanel>(isDestory: true);
                UIMgr.Instance.HidePanel<GamePanel>(isDestory: true);
                UIMgr.Instance.HidePanel<BagPanel>(isDestory: true);
                SceneMgr.Instance.LoadSceneAsyn("BeginScene");
                break;
        }
    }

    protected override void SliderValueChange(string sliderName, float value)
    {
        switch (sliderName)
        {
            case "sliderMusic":
                //�������ֵĴ�С
                MusicMgr.Instance.ChangeBKMusicValue(value);
                GetControl<Slider>(sliderName).value = value;
                break;
            case "sliderSound":
                //������Ч�Ĵ�С
                MusicMgr.Instance.ChangeSoundValue(value);
                GetControl<Slider>(sliderName).value = value;
                break;
        }
    }

    protected override void ToggleValueChange(string togName, bool value)
    {
        switch (togName)
        {
            case "togMusic":
                MusicMgr.Instance.PlayOrPauseMusic(value);
                GetControl<Toggle>(togName).isOn = value;
                break;
            case "togSound":
                MusicMgr.Instance.PlayOrPauseSound(value);
                GetControl<Toggle>(togName).isOn = value;
                break;
        }
    }
}
