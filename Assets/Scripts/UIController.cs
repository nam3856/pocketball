using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public TextMeshProUGUI turnText;

    void OnEnable()
    {
        GameManager.OnTurnChanged += UpdateTurnText;
    }

    void OnDisable()
    {
        GameManager.OnTurnChanged -= UpdateTurnText;
    }

    private void UpdateTurnText(string playerName)
    {
        turnText.text = "Player "+ playerName + "'s Turn";
    }
}
