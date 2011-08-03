using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using AwesomiumMono;

public class WebCoreHelper : MonoBehaviour
{
    public void Start()
    {
        InvokeRepeating("tick", 0, 0.020F);
        Debug.Log("Started WebCoreHelper!");
    }

    public void tick()
    {
        WebCore.Update();
    }
}