using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CueBallController : MonoBehaviour
{
    public Rigidbody CueBallRigidbody;
    public CueController cueController;
    private float respawnHeight= -2f;

    public void HitBall()
    {
        float hitPower = 1000f; 
        Vector3 hitDirection = -1f * new Vector3(cueController.CueDirection.x,0, cueController.CueDirection.z);
        cueController.isHitting = true;
        CueBallRigidbody.AddForce(hitDirection * hitPower);
    }

    public void Update()
    {
        if(transform.position.y < respawnHeight)
        {
            Respawn();
        }
        if (CueBallRigidbody.velocity.magnitude <= 0.05)
        {
            cueController.isHitting = false;
        }
        else cueController.isHitting = true;
    }
    public void Respawn()
    {
        transform.position = new Vector3 (-4.5f,0.5f);

        CueBallRigidbody.velocity = Vector3.zero;
        CueBallRigidbody.angularVelocity = Vector3.zero;
    }
    void OnGUI()
	{
		
        GUI.Label(new Rect(120, 70, 80, 20), $"{CueBallRigidbody.velocity.magnitude}");
    }
}
