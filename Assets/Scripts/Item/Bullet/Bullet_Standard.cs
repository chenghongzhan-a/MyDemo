using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet_Standard : BulletBase
{
    private float time;
    private void Update()
    {
        this.transform.Translate(this.transform.right * Time.deltaTime * speed, Space.World);
        time += Time.deltaTime;
        if (time >= 5)
        {
            Destroy(this.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        BaseDecoration bd = collision.gameObject.GetComponent<BaseDecoration>();
        MonsterBase mb = collision.gameObject.GetComponent<MonsterBase>();
        if (bd != null)
        {
            bd.TakeDamage((int)damage);
            Destroy(this.gameObject);
        }
        if (mb != null)
        {
            mb.TakeDamage((int)damage);
            Destroy(this.gameObject);
        }
    }
}
