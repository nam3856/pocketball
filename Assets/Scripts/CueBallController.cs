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
        float hitPower = 500f; 
        Vector3 hitDirection = -1f * new Vector3(cueController.CueDirection.x,0, cueController.CueDirection.z);
        cueController.isHitting = true;
        CueBallRigidbody.AddForce(hitDirection * hitPower);
    }

    public void Update()
    {
        if (CueBallRigidbody.velocity.magnitude <= 0.1)
        {
            cueController.isHitting = false;
        }
        else cueController.isHitting = true;
    }

    void OnGUI()
	{
		GUI.Label(new Rect(20, 70, 80, 20), $"{cueController.CueDirection}");
	}
}
