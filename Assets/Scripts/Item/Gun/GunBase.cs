using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GunBase : ItemBase
{
    public float damage;
    public float fireRate;
    public float fireTime;
    public float reloadTime;
    public AudioClip shootSound;
    public GameObject bullet;
    public Transform shotPoint;

    public abstract void Shoot();
    public abstract void Reload();
}
