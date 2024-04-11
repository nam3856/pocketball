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
    private string playerTurn = "1";
    public static event System.Action<string> OnTurnChanged;
    public bool hasExtraTurn = false;
    public bool FreeBall = false;
    public float minVelocityThreshold = 0.1f;
    private bool isCheckingStopped = false;
    public CueController cueController;
    private float checkInterval = 0.5f;
    private float lastCheckTime;

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
            if (stripedCount == 0)
            {
                if (!FreeBall)
                    Debug.Log("Striped Win");
                else
                    Debug.Log("Solid Win ");
            }
            else if (solidCount == 0)
            {
                if (!FreeBall)
                    Debug.Log("Solid Win");
                else
                    Debug.Log("Striped Win");
            }
            else
            {
                if (string.Equals(playerTurn, "1"))
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
        if (Time.time - lastCheckTime > checkInterval)
        {
            lastCheckTime = Time.time;
            CheckIfAllBallsStopped();
        }
        
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

        // ∏µÁ ∞¯¿Ã ∏ÿ√Ë¿Ω
        isCheckingStopped = true;
        if (cueController.isHitting)
        {
            cueController.isHitting = false;
            TurnChange();
        }
        
    }
    private void OnGUI()
    {
        GUI.Label(new Rect(20, 270, 80, 20), $"{stripedCount}, {solidCount}");
        GUI.Label(new Rect(20, 200, 100, 20), $"{isCheckingStopped} {playerTurn}");
    }
}
