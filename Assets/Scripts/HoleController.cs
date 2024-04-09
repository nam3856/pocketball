using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HoleController : MonoBehaviour
{
    Transform[] holes;

    private void Start()
    {
        GameObject[] holeObjects = GameObject.FindGameObjectsWithTag("Hole");
        holes = new Transform[holeObjects.Length];
        for(int i = 0; i < holeObjects.Length;i++)
        {
            holes[i] = holeObjects[i].transform; 
        }
    }
    
    
}
