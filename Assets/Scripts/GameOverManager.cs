using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public TextMeshProUGUI winnerText;

    void Start()
    {
        winnerText.text = "Winner: Player " + DataManager.Instance.WinnerName;
    }
}
