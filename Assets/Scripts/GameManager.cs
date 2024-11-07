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
    public static GameManager Instance { get; private set; } // �̱��� ����

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
        // �̱��� ���� ����
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �ʿ��� ��� �� ��ȯ �ÿ��� ����
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

        // ��� ���� BallController�� ������ ����Ʈ�� �߰�
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

        // �̺�Ʈ ������ OnNetworkSpawn������ �����մϴ�.
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
            Debug.Log("�� �̻� �÷��̾ ���� �� �����ϴ�.");
        }
    }

    private void OnSolidCountChanged(int previousValue, int newValue)
    {
        // Ŭ���̾�Ʈ ������ UI ������Ʈ �� ó��
    }

    private void OnStripedCountChanged(int previousValue, int newValue)
    {
        // Ŭ���̾�Ʈ ������ UI ������Ʈ �� ó��
    }

    private void OnPlayerTurnChanged(int previousValue, int newValue)
    {
        // Ŭ���̾�Ʈ ������ �� ���� ó��
        // ��: UI ������Ʈ �Ǵ� �Ͽ� ���� �Է� ó��
    }

    public int GetMyPlayerNumber()
    {
        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        if (player1ClientId.Value == myClientId)
            return 1;
        else if (player2ClientId.Value == myClientId)
            return 2;
        else
            return 0; // �Ҵ���� ����
    }

    string GetPlayerType(int playerId)
    {
        foreach (var playerType in playerTypes)
        {
            if (playerType.playerId == playerId)
                return playerType.type;
        }
        return null; // �Ǵ� "Not Assigned"
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
        // ������ ���� ���� ������ �˻� ���
        movementCheckCancellationTokenSource?.Cancel();
        movementCheckCancellationTokenSource = new CancellationTokenSource();

        // ���� �����̰� ������ ǥ��
        ballsAreMoving = true;
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        // ��� ���� ���� ������ �˻�
        while (!AreAllBallsStopped())
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: movementCheckCancellationTokenSource.Token);
        }

        // ���� ��� ������ �� ó��
        ballsAreMoving = false;

        // ������ �˻� ���
        movementCheckCancellationTokenSource.Cancel();
        movementCheckCancellationTokenSource = null;

        // �� ���� ó��
        ProcessTurnEnd();
    }

    public void BallFell(GameObject ball)
    {
        if (IsServer)
        {
            // ���������� ���� ���� ���� ���� ����
            ProcessBallFell(ball);
        }
        else
        {
            // Ŭ���̾�Ʈ������ �������� ��û
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
        // ���� �Ͽ� ���ϵ� ������ ����
        pocketedBallsThisTurn.Add(ball);

        if (ball.CompareTag("StripedBall"))
        {
            stripedCount.Value--;

            // �÷��̾� Ÿ���� �������� �ʾҴٸ� Ÿ���� �Ҵ�
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
            // 8�� �� ó�� ����
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

        // NetworkList �ʱ�ȭ
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
            // Ÿ���� �������� ���� 8�� ���� ������ ���� �¸�
            winner.Value = playerTurn.Value == 1 ? 2 : 1;
            EndGame();
        }
        else
        {
            // �÷��̾��� ��� ���� �� �־����� Ȯ��
            if ((GetPlayerType(playerTurn.Value) == "SolidBall" && solidCount.Value == 0) || (GetPlayerType(playerTurn.Value) == "StripedBall" && stripedCount.Value == 0))
            {
                winner.Value = playerTurn.Value;
                EndGame();
            }
            else
            {
                // �ڽ��� ���� �����ִ� ���¿��� 8�� ���� ������ ���� �¸�
                winner.Value = playerTurn.Value == 1 ? 2 : 1;
                EndGame();
            }
        }
    }

    void EndGame()
    {
        if (IsServer)
        {
            // �������� ���� ���� ���� ����
            Debug.Log($"Player {winner.Value} Wins!");
            // Ŭ���̾�Ʈ���� ���� ���� �˸�
            EndGameClientRpc(winner.Value);
        }
    }

    [ClientRpc]
    void EndGameClientRpc(int winnerPlayer)
    {
        // Ŭ���̾�Ʈ���� ���� ���� ó��
        Debug.Log($"Player {winnerPlayer} Wins!");
        DataManager.Instance.WinnerName = winnerPlayer;
        // �� ��ȯ �� �ʿ��� �۾� ����
        // sceneLoader.ChangeScene("end");
    }

    void TurnChange()
    {
        if (cueBallPocketed)
        {
            // ���濡�� ������ �ο�
            freeBall = true;
        }

        if (!hasExtraTurn)
        {
            // �� ����
            playerTurn.Value = playerTurn.Value == 1 ? 2 : 1;
            // Ŭ���̾�Ʈ���� �� ���� �˸�
            NotifyTurnChangedClientRpc(playerTurn.Value);
        }
        else
        {
            hasExtraTurn = false;
        }

        // ���� ���� ���� ���� �ʱ�ȭ
        isFirstTime = false;
        pocketedBallsThisTurn.Clear();
        cueBallPocketed = false;
    }

    [ClientRpc]
    void NotifyTurnChangedClientRpc(int newPlayerTurn)
    {
        // Ŭ���̾�Ʈ���� �� ���� ó��
        // ��: UI ������Ʈ
    }

    void ProcessTurnEnd()
    {
        if (!IsServer) return;
        // ť���� ���ϵǾ��� ��
        if (cueBallPocketed)
        {
            isFirstTime = false;
            hasExtraTurn = false; // �߰� �� ����
            TurnChange();
            return;
        }

        // ���ϵ� ���� ���� ���
        if (pocketedBallsThisTurn.Count == 0)
        {
            isFirstTime = false;
            hasExtraTurn = false;
            TurnChange();
            return;
        }

        // ���ϵ� �� �߿� 8�� ���� �ִ� ���� �̹� ó���Ǿ���

        // �÷��̾� Ÿ���� �������� �ʾ��� ��
        if (!isTypeAssigned)
        {
            if (!isFirstTime) AssignPlayerTypeBasedOnPocketedBalls();
        }
        else
        {
            // �ڽ��� ���� �ƴ� ���� �������� ���
            if (pocketedBallsThisTurn.Any(ball => ball.CompareTag(GetOpponentType(playerTurn.Value))))
            {
                hasExtraTurn = false;
                TurnChange();
                return;
            }
        }

        isFirstTime = false;
        // �߰� �� ���ο� ���� �� ����
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
        // ���ϵ� ���� �� �ָ���� ��Ʈ������ ���� Ȯ��
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
            // �� �� �־��ų� �ƹ��͵� ���� �ʾ��� ��� �� ����
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
