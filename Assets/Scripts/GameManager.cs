using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private List<GameObject> stripedBalls;
    private List<GameObject> solidBalls;
    private int solidCount = 7;
    private int stripedCount = 7;
    public GameObject eightBall;
    private int turn;
    void Start()
    {
        stripedBalls = new List<GameObject>(GameObject.FindGameObjectsWithTag("StripedBall"));
        solidBalls = new List<GameObject>(GameObject.FindGameObjectsWithTag("SolidBall"));
        
    }

    public void BallFell(GameObject ball)
    {
        if (ball.CompareTag("StripedBall"))
        {
            stripedCount--;
        }
        else if (ball.CompareTag("SolidBall"))
        {
            solidCount--;
        }
        else if (ball == eightBall)
        {
            if (stripedCount == 0)
            {
                Debug.Log("striped Win");
            }
            else if (solidCount == 0)
            {
                Debug.Log("Solid Win");
            }
            else
            {
                if (turn == 1)
                {
                    Debug.Log("Player 2 Win");
                }
                else
                {
                    Debug.Log("Player 1 Win");
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(20, 270, 80, 20), $"{stripedCount}, {solidCount}");
    }
}
