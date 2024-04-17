using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CueController : MonoBehaviour
{
    private Vector3 mousePos;
    private float angle = 0.0f;
    private bool isPullingBack = false;
    private float pullBackDistance = 2f;
    private float[] attackSpeed = new float[] { 5f, 12f };
    public bool isHitting = false;
    public CueBallController cueBallController;

    public Vector3 CueDirection;
    public GameObject Cue;
    public Transform CueBall;
    public Transform cueTransform;


    void Start()
    {

    }

    private bool isCursorInRange(Vector3 point, float range)
    {
        if (isHitting) return false;
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

            if (!(isPullingBack || isHitting))//당기고 있지 않거나 치고있지 않다면
            {
                mousePos.y = CueBall.transform.position.y;
                Vector3 targetDir = (mousePos - CueBall.position).normalized;
                angle = Mathf.Atan2(targetDir.x, targetDir.z) * Mathf.Rad2Deg;
                Cue.transform.eulerAngles = new(90, angle, 0);
                if (isCursorInRange(mousePos, 0.6f))
                {
                    Cue.transform.position = CueBall.position + 0.01f * targetDir;
                }
                else
                {
                    Cue.transform.position = new Vector3(mousePos.x, 1.0f, mousePos.z);
                }

            }
        }
        else if (!isPullingBack)
        {
            Cue.GetComponent<SpriteRenderer>().enabled = false;
        }

        if (!(isPullingBack || isHitting))
        {
            if (Input.GetMouseButtonDown(0))
            {
                isPullingBack = true;
                StartCoroutine(MoveCue(0));
            }
        }
        if (Input.GetMouseButtonUp(0) && isPullingBack)
        {
            StartCoroutine(MoveCue(1));
        }
    }    
    
    IEnumerator MoveCue(int index)
    {
        float startTime = Time.time;
        CueDirection = (cueTransform.position - CueBall.position).normalized;
        Vector3 startPosition = cueTransform.position;
        Vector3[] endPosition = new Vector3[] { CueBall.position + pullBackDistance * CueDirection, CueBall.position + 0.01f * CueDirection };
        float[] animtime = new float[] { 0.2f, 0.2f };
        while (Time.time - startTime < animtime[index])
        {
            cueTransform.position = Vector3.Lerp(startPosition, endPosition[index], (Time.time - startTime) * attackSpeed[index]);
            yield return null;
        }

        cueTransform.position = endPosition[index];

        if (index == 1)
        {
            if(!isHitting) cueBallController.HitBall();
            isPullingBack = false;
        }
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 250, 500), "Loader Menu");
        GUI.Label(new Rect(10, 100, 250, 20), $"Mouse {mousePos}");
        GUI.Label(new Rect(10, 120, 250, 20), $"Cue angle {angle}");
        GUI.Label(new Rect(10, 40, 250, 20), $"Cue pulling {isPullingBack}");
    }
}
