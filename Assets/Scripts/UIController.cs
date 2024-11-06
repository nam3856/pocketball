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
    }

    void OnDisable()
    {
        GameManager.OnScoreChanged -= UpdateScore;
    }

    private void UpdateTurnText(string playerName)
    {
        turnText.text = "Player "+ playerName + "'s Turn";
    }
    private void UpdateScore(int solid, int striped) {
        score.text = "Player 1 (Solid): " + solid + "\nPlayer 2 (Striped): " + striped;
    }
}
