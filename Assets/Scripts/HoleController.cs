using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HoleController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball") && other.gameObject.name.CompareTo("Cue Ball") != 0)
        {
            Debug.Log($"{other.gameObject.name} ºüÁü");
            Destroy(other.gameObject);
        }
    }
}
