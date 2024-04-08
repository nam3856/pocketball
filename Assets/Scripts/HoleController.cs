using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HoleController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"{other.gameObject.name} 진입");
        if (other.CompareTag("Ball") && other.gameObject.name.CompareTo("Cue Ball") != 0)
        {
            StartCoroutine(ByeBye(other));
        }
    }
    private IEnumerator ByeBye(Collider other)
    {
        Debug.Log($"{other.gameObject.name} 빠짐");
        // 1초 기다림
        yield return new WaitForSeconds(1f);
        other.gameObject.GetComponent<SphereCollider>().enabled = false;
        other.gameObject.GetComponent<Rigidbody>().mass = 0.2f;
        // 2초 기다림
        yield return new WaitForSeconds(2f);
        // 오브젝트 제거
        Destroy(other.gameObject);
    }
}
