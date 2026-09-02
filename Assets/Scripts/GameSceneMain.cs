using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSceneMain : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        InputMgr.Instance.StartOrCloseInputMgr(true);
        ABResMgr.Instance.LoadResAsync<GameObject>("camera", "PlayerCamera", (camera) =>
        {
            Instantiate(camera);
        }, true);

        InputMgr.Instance.ChangeKeyboardInfo(E_EventType.E_Left, KeyCode.Mouse0, InputInfo.E_InputType.Down);
        _ = MonsterSpawnManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
