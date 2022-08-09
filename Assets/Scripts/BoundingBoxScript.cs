using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;

public class BoundingBoxScript : MonoBehaviour
{
    int counter;
    private float initializationTime;
    //set time to live for 3D bounding box
    int TTL = 20;


    void Start()
    {
        //frame counter
        counter = 0;

    }

    
    void RemoveDetection()
    {

        Destroy(gameObject);
    }

    void Update()
    {
        counter += 1;
        //If object has existed for more than the given threshold without update, we treat it as stale and remove
        if (counter > TTL)
        {
            RemoveDetection();
        }

    }


}