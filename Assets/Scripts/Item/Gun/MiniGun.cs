using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGun : GunBase
{
    public override void Reload()
    {
        // 换弹逻辑
    }

    public override void Shoot()
    {
        // 射击逻辑
        if (fireTime > fireRate)
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                // 点击的是UI，不执行开枪
                return;
            }
            GameObject obj = Instantiate(bullet);
            obj.transform.position = shotPoint.position;
            obj.transform.rotation = shotPoint.rotation;
            obj.GetComponent<SpriteRenderer>().sortingLayerName = GetComponent<SpriteRenderer>().sortingLayerName;
            fireTime = 0;
        }
    }

    public override void OnLeftClick()
    {
        base.OnLeftClick();
        Shoot();
    }

    public override void OnRightClick()
    {
        // 不同的枪械 右键有不同的逻辑 不一定所有的枪械右键都有用
    }

    private void Update()
    {
        fireTime += Time.deltaTime;
    }
}
