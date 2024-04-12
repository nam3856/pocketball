using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private List<GameObject> stripedBalls;
    private List<GameObject> solidBalls;
    public List<Rigidbody> balls = new List<Rigidbody>();
    private int solidCount = 7;
    private int stripedCount = 7;
    public GameObject eightBall;
    private string playerTurn = "2";
    public static event System.Action<string> OnTurnChanged;
    public bool hasExtraTurn = false;
    public bool FreeBall = false;
    public float minVelocityThreshold = 0.1f;
    private bool isCheckingStopped = false;
    public CueController cueController;
    private float checkInterval = 0.5f;
    private float lastCheckTime;
    private bool[] winnerSwitch = new bool[6];
    public SceneLoader sceneLoader;

    void Start()
    {
        stripedBalls = new List<GameObject>(GameObject.FindGameObjectsWithTag("StripedBall"));
        solidBalls = new List<GameObject>(GameObject.FindGameObjectsWithTag("SolidBall"));
        balls.AddRange(FindObjectsOfType<Rigidbody>().Where(obj => obj.CompareTag("StripedBall") || obj.CompareTag("SolidBall") || obj.CompareTag("EightBall") || obj.CompareTag("CueBall")).ToList());

        Debug.Log($"Balls Count: {balls.Count()}");
    }

    public void BallFell(GameObject ball)
    {
        if (ball.CompareTag("StripedBall"))
        {
            stripedCount--;
            hasExtraTurn = true;
        }
        else if (ball.CompareTag("SolidBall"))
        {
            solidCount--;
            hasExtraTurn = true;
        }
        else if (ball.CompareTag("CueBall"))
        {
            FreeBall = true;
        }
        else if (ball == eightBall)
        {
            if (stripedCount == 0 && playerTurn=="2")
            {
                winnerSwitch[1] = true;
            }
            else if (solidCount == 0 && playerTurn == "1")
            {
                winnerSwitch[0] = true;
            }
            else if (stripedCount > 0 && playerTurn == "2")
            {
                winnerSwitch[2] = true;
            }
            else if (solidCount > 0 && playerTurn == "1")
            {
                winnerSwitch[3] = true;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - lastCheckTime > checkInterval)
        {
            lastCheckTime = Time.time;
            CheckIfAllBallsStopped();
        }
        
    }
    int WinnerCheck()
    {
        if (winnerSwitch[0] && !FreeBall)
        {
            Debug.Log("Player 1 Win");
            return 1;
        }
        else if (winnerSwitch[1] && !FreeBall)
        {
            Debug.Log("Player 2 Win");
            return 2;
        }
        else if (winnerSwitch[2])
        {
            Debug.Log("Player 1 Win (P2 Foul)");
            return 1;
        }
        else if (winnerSwitch[3])
        {
            Debug.Log("Player 2 Win (P1 Foul)");
            return 2;
        }
        else if (FreeBall)
        {
            if (winnerSwitch[0])
            {
                Debug.Log("Player 2 Win (P1 Foul)");
                return 2;
            }
            else if (winnerSwitch[1])
            {
                Debug.Log("Player 1 Win (P2 Foul)");
                return 1;
            }
        }

        TurnChange();
        return 0;
    }
    void TurnChange()
    {
        if (string.Equals(playerTurn, "1"))
            playerTurn = "2";
        else
            playerTurn = "1";
        OnTurnChanged?.Invoke(playerTurn);
    }

    void CheckIfAllBallsStopped()
    {
        foreach (Rigidbody ball in balls)
        {
            if (ball.velocity.sqrMagnitude > 0.001f)
            {
                isCheckingStopped = false;
                cueController.isHitting = true;
                return;
            }
        }

        // ¸ðµç °øÀÌ ¸ØÃèÀ½
        isCheckingStopped = true;
        if (cueController.isHitting)
        {
            cueController.isHitting = false;
            if (WinnerCheck()>0)
            {
                sceneLoader.ChangeScene("end");
            }
                
        }
        
    }
    private void OnGUI()
    {
        GUI.Label(new Rect(20, 270, 80, 20), $"{stripedCount}, {solidCount}");
        GUI.Label(new Rect(20, 200, 100, 20), $"{isCheckingStopped} {playerTurn}");
        GUI.Label(new Rect(20, 250, 150, 20), $"{winnerSwitch[0]} {winnerSwitch[1]} {winnerSwitch[2]} {winnerSwitch[3]}");
    }
}
