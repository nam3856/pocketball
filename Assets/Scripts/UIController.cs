using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI score;

    private void Start()
    {
        
    }

    public void SetUpTurnText()
    {
        if (GameManager.Instance != null)
        {
            // 현재 턴 값을 UI에 초기 설정
            UpdateTurnText(GameManager.Instance.playerTurn.Value);

            // 턴이 변경될 때마다 호출될 콜백 등록
            GameManager.Instance.playerTurn.OnValueChanged += OnPlayerTurnChanged;
        }
        else
        {
            Debug.LogError("UIController: GameManager 인스턴스를 찾을 수 없습니다.");
        }
    }

    private void UpdateTurnText(int turnIndex)
    {
        if (GameManager.Instance.players.Count >= turnIndex)
        {
            ulong playerId = GameManager.Instance.players[turnIndex-1];
            string playerName = GetPlayerName(playerId);
            turnText.text = $"Turn: Player {playerName}";
        }
        else
        {
            turnText.text = "Turn: Unknown";
        }
    }

    private string GetPlayerName(ulong playerId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    var playerNameComponent = client.PlayerObject.GetComponent<PlayerName>();
                    if (playerNameComponent != null)
                    {
                        return playerNameComponent.PlayerNameVar.Value.ToString();
                    }
                }
            }
        }
        return playerId.ToString(); // 기본적으로 ClientId 반환
    }

    private void OnPlayerTurnChanged(int previousValue, int newValue)
    {
        UpdateTurnText(newValue);
    }
    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerTurn.OnValueChanged -= OnPlayerTurnChanged;
        }
    }
    private void UpdateScore(int solid, int striped) {
        //score.text = "Player 1: " + solid + "\nPlayer 2: " + striped;
    }
}
