using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;

public class TestMove : MonoBehaviour
{
    public AnimationCurve curve;
    // Start is called before the first frame update
    async void Start()
    {
        //transform.DOMove(Vector3.one, 1).SetLoops(-1, LoopType.Incremental).SetRelative(true);
        //transform.DOMove(Vector3.one, 2).SetEase(curve);

        //Material material = GetComponent<MeshRenderer>().material;
        //material.DOBlendableColor(Color.red, 2);
        //material.DOBlendableColor(Color.blue, 2);     

        var tweener = transform.DOMove(Vector3.one * 2, 1).SetLoops(3);  
        await Task.Delay(TimeSpan.FromSeconds(1.1f));
        Debug.Log(tweener.CompletedLoops());
        await Task.Delay(TimeSpan.FromSeconds(1.1f));
        Debug.Log(tweener.CompletedLoops());
        await Task.Delay(TimeSpan.FromSeconds(1.1f));
        Debug.Log(tweener.CompletedLoops());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
