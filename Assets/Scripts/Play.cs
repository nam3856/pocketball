using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Play : MonoBehaviour
{
	public GameObject CueBall;
	public GameObject Cue;
	private Vector3 mousePos;

    private float angle = 0.0f;

    private void Start()
	{
		//오브젝트 생성
		//CueBall = Instantiate(CueBall, new Vector3(0, 0, 0), Quaternion.identity);
	}
	private bool isCursorInRange(Vector3 point, float range)
    {
		if (point.x <= CueBall.transform.position.x + range && point.x >= CueBall.transform.position.x - range && point.z <= CueBall.transform.position.z + range && point.z >= CueBall.transform.position.z - range)
			return true;

		return false;
	}




	private void Update()
	{
		mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

		if (isCursorInRange(mousePos, 2.0f))
        {
			Cue.GetComponent<SpriteRenderer>().enabled = true;
            if (isCursorInRange(mousePos, 0.6f))
			{
                Cue.transform.position = CueBall.transform.position;
            }
			else
			{
                Cue.transform.position = new Vector3(mousePos.x, 1.0f, mousePos.z);
            }
            mousePos.y = CueBall.transform.position.y;
            Vector3 targetDir = mousePos - CueBall.transform.position;
            angle = Mathf.Atan2(targetDir.x, targetDir.z) * Mathf.Rad2Deg;
			Cue.transform.eulerAngles = new (90, angle, 0);
		}
        else
        {
			Cue.GetComponent<SpriteRenderer>().enabled = false;
		}
		if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
		{
			PlayAnimation();
		}
		else if (Input.GetMouseButtonUp(0))
		{
			
		}
	}

	private void PlayAnimation()
	{

	}

    public void HitBall()
	{
		CueBall.GetComponent<Rigidbody>().AddForce(new(5,0,5), ForceMode.Impulse);

    }

    void OnGUI()
	{
		// Make a background box
		GUI.Box(new Rect(10, 10, 100, 90), "Loader Menu");
        GUI.Label(new Rect(10, 100, 200, 20), $"{mousePos}");
        GUI.Label(new Rect(10, 150, 200, 20), $"{angle}" );

		// Make the first button. If it is pressed, Application.Loadlevel (1) will be executed
		if (GUI.Button(new Rect(20, 40, 80, 20), "Level 1"))
		{
			Debug.Log("hi");
		}

		// Make the second button.
		if (GUI.Button(new Rect(20, 70, 80, 20), "Level 2"))
		{
			Debug.Log("hello");
		}
	}
}
