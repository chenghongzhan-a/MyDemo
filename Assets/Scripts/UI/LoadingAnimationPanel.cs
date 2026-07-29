using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingAnimationPanel : BasePanel
{
    public Image image;
    public TMP_Text text;
    public override void HideMe()
    {
        
    }

    public override void ShowMe()
    {
        // 1.加载存档数据
        PlayerArchiveInfo archive = BinaryDataMgr.Instance.Load<PlayerArchiveInfo>(ArchiveManager.Instance.currentArchiveKey);
        ArchiveManager.Instance.currentArchive = archive;

        // 2.提取 archiveId
        string archiveId = ArchiveManager.Instance.currentArchiveKey.Replace("Archive_", "");
        ArchiveManager.Instance.id = int.Parse(archiveId);
        // 3.加载世界修改数据
        ArchiveManager.Instance.Load(int.Parse(archiveId));

        StartCoroutine(LoadSceneAsync("GameScene"));
    }


    IEnumerator LoadSceneAsync(string sceneName)
    {
        //根据PlayerArchiveInfo里面的种子 以及玩家上次所在位置来生产世界

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            image.fillAmount= progress;
            text.text = "进度: " + (int)progress * 100 + "%";
            yield return null;

        }

        yield return new WaitForSeconds(1);
        yield return asyncLoad;
        UIMgr.Instance.HidePanel<LoadingAnimationPanel>();
        UIMgr.Instance.ShowPanel<GamePanel>();
    }
}
