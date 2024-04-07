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
        float hitPower = 500f; // ���� ġ�� ���� ũ��
        // ť���� ������ �̵� ������ ������� ���� ���� ����
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
