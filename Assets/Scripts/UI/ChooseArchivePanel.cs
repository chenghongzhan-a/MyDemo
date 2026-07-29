using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChooseArchivePanel : BasePanel
{
    public string nowChooseArchiveName;

    public override void HideMe()
    {
        
    }

    public override void ShowMe()
    {
        ReFreshArchive();
    }

    protected override void ClickBtn(string btnName)
    {
        switch (btnName)
        {
            case "btnBegin":
                //开始游戏
                //加载选中的存档对应的数据
                //角色数据和地图数据应该分开 应为这样才能联机
                //播放加载动画 异步加载进度条
                if (nowChooseArchiveName == "")
                {
                    break;
                }

                UIMgr.Instance.HidePanel<ChooseArchivePanel>();
                UIMgr.Instance.ShowPanel<LoadingAnimationPanel>();
                break;
            case "btnBack":
                UIMgr.Instance.HidePanel<ChooseArchivePanel>();
                UIMgr.Instance.ShowPanel<MainPanel>();
                break;
            case "btnCreate":
                UIMgr.Instance.ShowPanel<CreateArchivePanel>();
                break;
            case "btnDelete":
                //删除存档
                UIMgr.Instance.ShowPanel<TipDeletePanel>();
                break;
        }
    }


    public void ReFreshArchive()
    {
        List<string> archiveList = ArchiveManager.Instance.GetArchiveList();
        ScrollRect scrollRect = GetControl<ScrollRect>("svChooseArchive");
        RectTransform content = scrollRect.content;

        // 清空旧内容
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // 设置 Content 高度
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 115 * archiveList.Count);

        // 获取或创建 ToggleGroup
        ToggleGroup toggleGroup = content.GetComponent<ToggleGroup>();
        if (toggleGroup == null)
        {
            toggleGroup = content.gameObject.AddComponent<ToggleGroup>();
        }
        toggleGroup.allowSwitchOff = true;

        // 遍历创建（同步加载）
        for (int i = 0; i < archiveList.Count; i++)
        {
            string archiveKey = archiveList[i];
            int index = i;

            //加载 GameObject
            ABResMgr.Instance.LoadResAsync<GameObject>("ui", "togChooseArchive", (prefab) =>
            {
                // 因为是同步，这里会立即执行

                // 检查预制体
                if (prefab == null)
                {
                    Debug.LogError("预制体加载失败！");
                    return;
                }

                // 实例化
                GameObject toggleObj = Instantiate(prefab, content);

                // 设置位置
                RectTransform rect = toggleObj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(0, -index * 115);
                }

                // 获取 TogChooseArchive 组件
                TogChooseArchive archiveToggle = toggleObj.GetComponent<TogChooseArchive>();
                if (archiveToggle == null)
                {
                    Debug.LogError("预制体上缺少 TogChooseArchive 组件！");
                    Destroy(toggleObj);
                    return;
                }

                //加载存档数据（正确的数据类）
                PlayerArchiveInfo archiveData = BinaryDataMgr.Instance.Load<PlayerArchiveInfo>(archiveKey);
                if (archiveData == null)
                {
                    Debug.LogWarning($"加载存档数据失败: {archiveKey}");
                    return;
                }

                //设置 UI 显示
                if (archiveToggle.txtName != null)
                    archiveToggle.txtName.text = archiveData.name;

                if (archiveToggle.txtCreateTime != null)
                    archiveToggle.txtCreateTime.text = $"创建时间: {archiveData.createTime}";

                if (archiveToggle.txtLastLogTime != null)
                    archiveToggle.txtLastLogTime.text = $"上次登录: {archiveData.lastLogTime}";

                //设置 Toggle
                Toggle toggle = toggleObj.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.group = toggleGroup;
                    toggle.onValueChanged.AddListener((isOn) =>
                    {
                        if (isOn)
                        {
                            Debug.Log($"选择了存档: {archiveKey}");
                            nowChooseArchiveName = archiveKey;
                            ArchiveManager.Instance.currentArchiveKey = nowChooseArchiveName;
                        }
                        else
                        {
                            Debug.Log($"取消了选择: {archiveKey}");
                            nowChooseArchiveName = "";
                            ArchiveManager.Instance.currentArchiveKey = "";
                        }
                    });
                }

            }, true); // 同步加载
        }
    }
}
