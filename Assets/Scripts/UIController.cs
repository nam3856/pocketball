using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI score;

    void OnEnable()
    {
        GameManager.OnScoreChanged += UpdateScore;
        GameManager.OnTurnChanged += UpdateTurnText;
    }

    void OnDisable()
    {
        GameManager.OnScoreChanged -= UpdateScore;
        GameManager.OnTurnChanged -= UpdateTurnText;
    }

    private void UpdateTurnText(int playerName)
    {
        turnText.text = "Player "+ playerName + "'s Turn";
    }
    private void UpdateScore(int solid, int striped) {
        //score.text = "Player 1: " + solid + "\nPlayer 2: " + striped;
    }
}
