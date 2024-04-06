using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Play : MonoBehaviour
{
	private static readonly int GRAVITY_SCALE = 0;

	public GameObject CueBall;
	public GameObject Cue;

	private void Start()
	{
		//오브젝트 생성
		//CueBall = Instantiate(CueBall, new Vector3(0, 0, 0), Quaternion.identity);
	}
	private bool isCursorInRange(Vector3 point, float range)
    {
		if (point.x <= CueBall.transform.position.x + range && point.x >= CueBall.transform.position.x - range && point.y <= CueBall.transform.position.y + range && point.y >= CueBall.transform.position.y - range)
			return true;
		return false;
	}

	private void Update()
	{
		Vector3 point = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));

		if (isCursorInRange(point, 5.0f))
        {
			point.z = -3.4f;
			Cue.transform.position = point;
        }
		if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
		{
			//ObjectMove();
		}
		else if (Input.GetMouseButtonUp(0))
		{
			//ObjectDrop();
		}
	}

	private void ObjectMove()
	{
		// Screen 좌표계인 mousePosition을 World 좌표계로 바꾼다
		Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

		// 오브젝트는 x로만 움직여야 하기 때문에 y는 고정
		mousePos.z = CueBall.transform.position.z;

		CueBall.transform.position = mousePos;
	}

	private void ObjectDrop()
	{
	}
}
