using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Touch : MonoBehaviour
{
    public GameObject rangeOb;
    SphereCollider rangeCol;

    private void Awake()
    {
        rangeCol = rangeOb.GetComponent<SphereCollider>();
    }

    private void OnMouseDown()
    {
        Vector2 originpo = rangeOb.transform.position;
        Debug.Log("클릭된 오브젝트 : " + gameObject.name);
        float range_x = rangeCol.bounds.size.x;
        float range_y = rangeCol.bounds.size.y;
        float range_z = rangeCol.bounds.size.z;

        Vector3 Vc = new Vector3(range_x, range_y, range_z);
    }
}
