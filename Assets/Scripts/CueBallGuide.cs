using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CueBallGuide : MonoBehaviour
{
    public Transform CueBall;
    public LineRenderer lineRenderer;
    public LayerMask collisionLayer;
    public float maxGuideLength = 5f;
    public CueController cueController;

    private void Start()
    {

        //collisionLayer = LayerMask.GetMask("collision");
    }
    void Update()
    {
        Debug.Log("Drawing guide line");
        DrawCueGuide();
    }

    void DrawCueGuide()
    {
        Vector3 start = CueBall.position;
        Vector3 direction = -1f * new Vector3(cueController.CueDirection.x, 0, cueController.CueDirection.z);

        RaycastHit hit;

        if(Physics.Raycast(start, direction, out hit, maxGuideLength, collisionLayer)) 
        {
            Debug.Log($"Hit: {hit.collider.name}");
            if (hit.collider.CompareTag("Wall"))
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, hit.point);

                Vector3 reflectedDirection = Vector3.Reflect(direction, hit.normal);
                if(Physics.Raycast(hit.point, reflectedDirection, out RaycastHit reflectHit, maxGuideLength, collisionLayer))
                {
                    lineRenderer.positionCount = 3;
                    lineRenderer.SetPosition(2,reflectHit.point);
                }
            }

            else if (hit.collider.CompareTag("Ball") || hit.collider.CompareTag("Hole"))
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, start + direction * maxGuideLength);
            }
        }
        else
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0 , start);
            lineRenderer.SetPosition(1, start + direction * maxGuideLength);
            Debug.Log($"{lineRenderer.positionCount}");
        }
    }
}
