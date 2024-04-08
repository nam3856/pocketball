using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HoleController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"{other.gameObject.name} ����");
        if (other.CompareTag("Ball") && other.gameObject.name.CompareTo("Cue Ball") != 0)
        {
            StartCoroutine(ByeBye(other));
        }
    }
    private IEnumerator ByeBye(Collider other)
    {
        Debug.Log($"{other.gameObject.name} ����");
        // 1�� ��ٸ�
        yield return new WaitForSeconds(1f);
        other.gameObject.GetComponent<SphereCollider>().enabled = false;
        other.gameObject.GetComponent<Rigidbody>().mass = 0.2f;
        // 2�� ��ٸ�
        yield return new WaitForSeconds(2f);
        // ������Ʈ ����
        Destroy(other.gameObject);
    }
}
