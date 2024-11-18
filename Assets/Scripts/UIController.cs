using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI playerTypeText;
    public GameObject loadingPanel;
    private GameManager gameManager;

    void Start()
    {
        WaitUntilStart().Forget();
    }
    
    async UniTaskVoid WaitUntilStart()
    {
        gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            await UniTask.WaitUntil(() => gameManager.playerTurn.Value >= 1);
            // 초기 점수 설정
            UpdateScore(gameManager.solidCount.Value, gameManager.stripedCount.Value);

            // GameManager의 점수 변경 이벤트에 구독
            gameManager.solidCount.OnValueChanged += OnSolidCountChanged;
            gameManager.stripedCount.OnValueChanged += OnStripedCountChanged;
            gameManager.player1Type.OnValueChanged += OnplayerTypeChanged;
            gameManager.player2Type.OnValueChanged += OnplayerTypeChanged;
        }
        else
        {
            Debug.LogError("UIController: GameManager 인스턴스를 찾을 수 없습니다.");
        }
        loadingPanel.SetActive(false);
    }

    private void OnplayerTypeChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        UpdatePlayerType();
    }

    public void UpdateTurnText(int turnIndex)
    {
        if (GameManager.Instance.players.Count >= turnIndex)
        {
            ulong playerId = GameManager.Instance.players[turnIndex - 1];
            string playerName = GetPlayerName(playerId);
            turnText.text = $"{playerName} 님의 차례";
        }
        else
        {
            turnText.text = "게임 시작 전";
        }
    }

    private string GetPlayerName(ulong playerId)
    {
        var gameSettings = GameSettings.Instance;
        if (gameSettings.clientPlayerNames.TryGetValue(playerId, out string playerName))
        {
            return playerName;
        }

        // 이름을 찾지 못한 경우 기본적으로 ClientId를 문자열로 반환합니다
        return playerId.ToString();
    }


    private void OnSolidCountChanged(int previousValue, int newValue)
    {
        UpdateScore(newValue, gameManager.stripedCount.Value);
        Debug.Log("UIController: OnSolidCountChanged");
    }

    private void OnStripedCountChanged(int previousValue, int newValue)
    {
        UpdateScore(gameManager.solidCount.Value, newValue);
        Debug.Log("UIController: OnStripedCountChanged");
    }
    
    public void UpdateScore(int solid, int striped)
    {
        scoreText.text = $"Solid Balls: {solid}\nStriped Balls: {striped}";
    }

    public void UpdatePlayerType()
    {
        int myPlayerNumber = gameManager.GetMyPlayerNumber();
        string playerType = gameManager.GetPlayerType(myPlayerNumber);

        if (!string.IsNullOrEmpty(playerType))
        {
            playerTypeText.text = $"Your Type: {playerType}";
        }
        else
        {
            playerTypeText.text = "Your Type: Not Assigned";
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            gameManager.solidCount.OnValueChanged -= OnSolidCountChanged;
            gameManager.stripedCount.OnValueChanged -= OnStripedCountChanged;
        }
    }
}
