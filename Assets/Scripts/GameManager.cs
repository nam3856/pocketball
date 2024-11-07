using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using Unity.Netcode;

[Serializable]
public class PlayerType : INetworkSerializable
{
    public int playerId;
    public string type;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerId);
        serializer.SerializeValue(ref type);
    }
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; } // 싱글톤 패턴

    private List<GameObject> stripedBalls;
    private List<GameObject> solidBalls;
    public List<BallController> ballControllers = new List<BallController>();

    public GameObject eightBall;
    public CueController cueController;
    public SceneLoader sceneLoader;

    public NetworkVariable<int> solidCount = new NetworkVariable<int>(7);
    public NetworkVariable<int> stripedCount = new NetworkVariable<int>(7);
    public NetworkVariable<int> playerTurn = new NetworkVariable<int>(1);
    private NetworkVariable<int> winner = new NetworkVariable<int>(0);
    public NetworkDictionary<int, PlayerType> playerTypes = new NetworkDictionary<int, PlayerType>();

    public NetworkVariable<ulong> player1ClientId = new NetworkVariable<ulong>();
    public NetworkVariable<ulong> player2ClientId = new NetworkVariable<ulong>();

    public bool hasExtraTurn = false;
    public bool freeBall = false;
    public bool ballsAreMoving = false;

    private bool isTypeAssigned = false;
    private bool isFirstTime = true;
    private List<GameObject> pocketedBallsThisTurn = new List<GameObject>();
    private bool cueBallPocketed = false;
    private CancellationTokenSource movementCheckCancellationTokenSource;

    void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 필요한 경우 씬 전환 시에도 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        stripedBalls = new List<GameObject>(GameObject.FindGameObjectsWithTag("StripedBall"));
        solidBalls = new List<GameObject>(GameObject.FindGameObjectsWithTag("SolidBall"));

        // 모든 공의 BallController를 가져와 리스트에 추가
        var ballObjects = GameObject.FindGameObjectsWithTag("StripedBall")
            .Concat(GameObject.FindGameObjectsWithTag("SolidBall"))
            .Concat(GameObject.FindGameObjectsWithTag("EightBall"))
            .Concat(GameObject.FindGameObjectsWithTag("CueBall"));
        foreach (var obj in ballObjects)
        {
            var ballController = obj.GetComponent<BallController>();
            if (ballController != null)
            {
                ballControllers.Add(ballController);
            }
        }
        isFirstTime = true;
        Debug.Log($"Balls Count: {ballControllers.Count}");

        if (IsServer)
        {
            solidCount.Value = 7;
            stripedCount.Value = 7;
            playerTurn.Value = 1;
        }

        // 이벤트 구독은 OnNetworkSpawn에서만 수행합니다.
    }

    public override void OnNetworkSpawn()
    {
        solidCount.OnValueChanged += OnSolidCountChanged;
        stripedCount.OnValueChanged += OnStripedCountChanged;
        playerTurn.OnValueChanged += OnPlayerTurnChanged;

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (player1ClientId.Value == 0)
        {
            player1ClientId.Value = clientId;
            Debug.Log($"Player 1 connected: {clientId}");
        }
        else if (player2ClientId.Value == 0)
        {
            player2ClientId.Value = clientId;
            Debug.Log($"Player 2 connected: {clientId}");
        }
        else
        {
            Debug.Log("더 이상 플레이어를 받을 수 없습니다.");
        }
    }

    private void OnSolidCountChanged(int previousValue, int newValue)
    {
        // 클라이언트 측에서 UI 업데이트 등 처리
    }

    private void OnStripedCountChanged(int previousValue, int newValue)
    {
        // 클라이언트 측에서 UI 업데이트 등 처리
    }

    private void OnPlayerTurnChanged(int previousValue, int newValue)
    {
        // 클라이언트 측에서 턴 변경 처리
        // 예: UI 업데이트 또는 턴에 따른 입력 처리
    }

    public int GetMyPlayerNumber()
    {
        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        if (player1ClientId.Value == myClientId)
            return 1;
        else if (player2ClientId.Value == myClientId)
            return 2;
        else
            return 0; // 할당되지 않음
    }

    string GetPlayerType(int playerId)
    {
        foreach (var playerType in playerTypes)
        {
            if (playerType.playerId == playerId)
                return playerType.type;
        }
        return null; // 또는 "Not Assigned"
    }

    public bool AreAllBallsStopped()
    {
        foreach (BallController ball in ballControllers)
        {
            if (!ball.IsBallStopped())
            {
                return false;
            }
        }
        foreach (BallController ball in ballControllers)
        {
            ball.BallRigidbody.velocity = Vector3.zero;
        }
        return true;
    }

    public async UniTaskVoid CheckBallsMovementAsync()
    {
        // 이전에 동작 중인 움직임 검사 취소
        movementCheckCancellationTokenSource?.Cancel();
        movementCheckCancellationTokenSource = new CancellationTokenSource();

        // 공이 움직이고 있음을 표시
        ballsAreMoving = true;
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        // 모든 공이 멈출 때까지 검사
        while (!AreAllBallsStopped())
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: movementCheckCancellationTokenSource.Token);
        }

        // 공이 모두 멈췄을 때 처리
        ballsAreMoving = false;

        // 움직임 검사 취소
        movementCheckCancellationTokenSource.Cancel();
        movementCheckCancellationTokenSource = null;

        // 턴 종료 처리
        ProcessTurnEnd();
    }

    public void BallFell(GameObject ball)
    {
        if (IsServer)
        {
            // 서버에서만 게임 상태 변경 로직 실행
            ProcessBallFell(ball);
        }
        else
        {
            // 클라이언트에서는 서버에게 요청
            BallFellServerRpc(new NetworkObjectReference(ball));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void BallFellServerRpc(NetworkObjectReference ballReference)
    {
        if (ballReference.TryGet(out NetworkObject ballNetworkObject))
        {
            GameObject ball = ballNetworkObject.gameObject;
            ProcessBallFell(ball);
        }
    }

    void ProcessBallFell(GameObject ball)
    {
        // 현재 턴에 포켓된 공들을 저장
        pocketedBallsThisTurn.Add(ball);

        if (ball.CompareTag("StripedBall"))
        {
            stripedCount.Value--;

            // 플레이어 타입이 정해지지 않았다면 타입을 할당
            if (!isTypeAssigned && !isFirstTime)
            {
                AssignPlayerType("StripedBall");
            }

            if (!IsPlayerTypeAssigned(playerTurn.Value)) hasExtraTurn = true;
            else if (GetPlayerType(playerTurn.Value) == "StripedBall")
            {
                hasExtraTurn = true;
            }
        }
        else if (ball.CompareTag("SolidBall"))
        {
            solidCount.Value--;

            if (!isTypeAssigned && !isFirstTime)
            {
                AssignPlayerType("SolidBall");
            }

            if (!IsPlayerTypeAssigned(playerTurn.Value)) hasExtraTurn = true;
            else if (GetPlayerType(playerTurn.Value) == "SolidBall")
            {
                hasExtraTurn = true;
            }
        }
        else if (ball.CompareTag("CueBall"))
        {
            cueBallPocketed = true;
            freeBall = true;
        }
        else if (ball == eightBall)
        {
            // 8번 공 처리 로직
            HandleEightBallPocketed();
        }
    }

    bool IsPlayerTypeAssigned(int playerId)
    {
        return GetPlayerType(playerId) != null;
    }

    void AssignPlayerType(string ballType)
    {
        isTypeAssigned = true;

        int currentPlayer = playerTurn.Value;
        int otherPlayer = currentPlayer == 1 ? 2 : 1;
        string otherBallType = ballType == "SolidBall" ? "StripedBall" : "SolidBall";

        // NetworkList 초기화
        playerTypes.Clear();
        playerTypes.Add(new PlayerType { playerId = currentPlayer, type = ballType });
        playerTypes.Add(new PlayerType { playerId = otherPlayer, type = otherBallType });

        Debug.Log($"Player {currentPlayer} is assigned {ballType}");
        Debug.Log($"Player {otherPlayer} is assigned {otherBallType}");
    }

    void HandleEightBallPocketed()
    {
        if (!isTypeAssigned)
        {
            // 타입이 정해지기 전에 8번 공을 넣으면 상대방 승리
            winner.Value = playerTurn.Value == 1 ? 2 : 1;
            EndGame();
        }
        else
        {
            // 플레이어의 모든 공을 다 넣었는지 확인
            if ((GetPlayerType(playerTurn.Value) == "SolidBall" && solidCount.Value == 0) || (GetPlayerType(playerTurn.Value) == "StripedBall" && stripedCount.Value == 0))
            {
                winner.Value = playerTurn.Value;
                EndGame();
            }
            else
            {
                // 자신의 공이 남아있는 상태에서 8번 공을 넣으면 상대방 승리
                winner.Value = playerTurn.Value == 1 ? 2 : 1;
                EndGame();
            }
        }
    }

    void EndGame()
    {
        if (IsServer)
        {
            // 서버에서 게임 종료 로직 실행
            Debug.Log($"Player {winner.Value} Wins!");
            // 클라이언트에게 게임 종료 알림
            EndGameClientRpc(winner.Value);
        }
    }

    [ClientRpc]
    void EndGameClientRpc(int winnerPlayer)
    {
        // 클라이언트에서 게임 종료 처리
        Debug.Log($"Player {winnerPlayer} Wins!");
        DataManager.Instance.WinnerName = winnerPlayer;
        // 씬 전환 등 필요한 작업 수행
        // sceneLoader.ChangeScene("end");
    }

    void TurnChange()
    {
        if (cueBallPocketed)
        {
            // 상대방에게 프리볼 부여
            freeBall = true;
        }

        if (!hasExtraTurn)
        {
            // 턴 변경
            playerTurn.Value = playerTurn.Value == 1 ? 2 : 1;
            // 클라이언트에게 턴 변경 알림
            NotifyTurnChangedClientRpc(playerTurn.Value);
        }
        else
        {
            hasExtraTurn = false;
        }

        // 다음 턴을 위해 변수 초기화
        isFirstTime = false;
        pocketedBallsThisTurn.Clear();
        cueBallPocketed = false;
    }

    [ClientRpc]
    void NotifyTurnChangedClientRpc(int newPlayerTurn)
    {
        // 클라이언트에서 턴 변경 처리
        // 예: UI 업데이트
    }

    void ProcessTurnEnd()
    {
        if (!IsServer) return;
        // 큐볼이 포켓되었을 때
        if (cueBallPocketed)
        {
            isFirstTime = false;
            hasExtraTurn = false; // 추가 턴 없음
            TurnChange();
            return;
        }

        // 포켓된 공이 없는 경우
        if (pocketedBallsThisTurn.Count == 0)
        {
            isFirstTime = false;
            hasExtraTurn = false;
            TurnChange();
            return;
        }

        // 포켓된 공 중에 8번 공이 있는 경우는 이미 처리되었음

        // 플레이어 타입이 정해지지 않았을 때
        if (!isTypeAssigned)
        {
            if (!isFirstTime) AssignPlayerTypeBasedOnPocketedBalls();
        }
        else
        {
            // 자신의 공이 아닌 공을 포켓했을 경우
            if (pocketedBallsThisTurn.Any(ball => ball.CompareTag(GetOpponentType(playerTurn.Value))))
            {
                hasExtraTurn = false;
                TurnChange();
                return;
            }
        }

        isFirstTime = false;
        // 추가 턴 여부에 따라 턴 변경
        if (!hasExtraTurn)
        {
            TurnChange();
        }
        else
        {
            hasExtraTurn = false;
            pocketedBallsThisTurn.Clear();
        }

        NotifyTurnChangedClientRpc(playerTurn.Value);
    }

    string GetOpponentType(int playerId)
    {
        int opponentId = playerId == 1 ? 2 : 1;
        return GetPlayerType(opponentId);
    }

    void AssignPlayerTypeBasedOnPocketedBalls()
    {
        // 포켓된 공들 중 솔리드와 스트라이프 개수 확인
        int solidPocketed = pocketedBallsThisTurn.Count(ball => ball.CompareTag("SolidBall"));
        int stripedPocketed = pocketedBallsThisTurn.Count(ball => ball.CompareTag("StripedBall"));

        if (solidPocketed > 0 && stripedPocketed == 0)
        {
            AssignPlayerType("SolidBall");
            hasExtraTurn = true;
        }
        else if (stripedPocketed > 0 && solidPocketed == 0)
        {
            AssignPlayerType("StripedBall");
            hasExtraTurn = true;
        }
        else
        {
            // 둘 다 넣었거나 아무것도 넣지 않았을 경우 턴 변경
            hasExtraTurn = false;
            TurnChange();
        }
    }

    public override void OnDestroy()
    {
        movementCheckCancellationTokenSource?.Cancel();
        solidCount.OnValueChanged -= OnSolidCountChanged;
        stripedCount.OnValueChanged -= OnStripedCountChanged;
        playerTurn.OnValueChanged -= OnPlayerTurnChanged;

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
        }

        base.OnDestroy();
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 170, 250, 20), $"Ball Count - Solid: {solidCount.Value}, Striped: {stripedCount.Value}");
        GUI.Label(new Rect(10, 190, 250, 20), $"Player {playerTurn.Value}'s Turn");
        GUI.Label(new Rect(10, 210, 250, 20), $"Player 1 Type: {GetPlayerType(1) ?? "Not Assigned"}");
        GUI.Label(new Rect(10, 230, 250, 20), $"Player 2 Type: {GetPlayerType(2) ?? "Not Assigned"}");
        GUI.Label(new Rect(10, 250, 250, 20), $"isFirst: {isFirstTime}");
    }
}
