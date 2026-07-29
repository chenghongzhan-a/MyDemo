using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    public override void HideMe()
    {
        
    }

    public override void ShowMe()
    {
        SliderValueChange("sliderMusic", PlayerPrefs.GetFloat("bkMusicValue"));
        SliderValueChange("sliderSound", PlayerPrefs.GetFloat("soundValue"));
        ToggleValueChange("togMusic", PlayerPrefs.GetInt("BKMusic") == 1 ? true : false);
        ToggleValueChange("togSound", PlayerPrefs.GetInt("Sound") == 1 ? true : false);
    }

    protected override void ClickBtn(string btnName)
    {
        switch (btnName)
        {
            case "btnClose":
                UIMgr.Instance.HidePanel<SettingPanel>();
                UIMgr.Instance.ShowPanel<MainPanel>();
                break;
        }
    }

    protected override void SliderValueChange(string sliderName, float value)
    {
        switch (sliderName)
        {
            case "sliderMusic":
                //控制音乐的大小
                MusicMgr.Instance.ChangeBKMusicValue(value);
                GetControl<Slider>(sliderName).value = value;
                break;
            case "sliderSound":
                //控制音效的大小
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
