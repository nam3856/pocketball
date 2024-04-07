using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CueBallController : MonoBehaviour
{
    public Rigidbody CueBallRigidbody;
    public CueController cueController;

    public void HitBall()
    {
        float hitPower = 500f; // 공을 치는 힘의 크기
        // 큐대의 마지막 이동 방향을 기반으로 공에 힘을 가함
        Vector3 hitDirection = cueController.CueDirection;
        cueController.isHitting = true;
        CueBallRigidbody.AddForce(hitDirection * hitPower);
    }

    public void Update()
    {
        if (CueBallRigidbody.velocity.magnitude <= 0.1)
        {
            cueController.isHitting = false;
        }
    }

    void OnGUI()
	{
		GUI.Label(new Rect(20, 70, 80, 20), $"{cueController.CueDirection}");
	}
}
