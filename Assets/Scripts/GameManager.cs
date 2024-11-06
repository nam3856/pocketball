using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private List<GameObject> stripedBalls;
    private List<GameObject> solidBalls;
    public List<BallController> ballControllers = new List<BallController>();
    private int solidCount = 7;
    private int stripedCount = 7;
    public GameObject eightBall;
    private int playerTurn = 1;
    public static event System.Action<int> OnTurnChanged;
    public static event System.Action<int, int> OnScoreChanged;
    public bool hasExtraTurn = false;
    public bool freeBall = false;
    public float minVelocityThreshold = 0.1f;
    public CueController cueController;
    private float checkInterval = 1f;
    private float lastCheckTime;
    private bool[] winnerSwitch = new bool[6];
    public SceneLoader sceneLoader;
    private int winner = 0;

    public bool ballsAreMoving = false;

    private bool isTypeAssigned = false;
    private bool isFirstTime = true;
    private Dictionary<int, string> playerType = new Dictionary<int, string>();
    private List<GameObject> pocketedBallsThisTurn = new List<GameObject>();
    private bool cueBallPocketed = false;

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
        return true;
    }

    public void BallFell(GameObject ball)
    {
        // ���� �Ͽ� ���ϵ� ������ ����
        pocketedBallsThisTurn.Add(ball);

        if (ball.CompareTag("StripedBall"))
        {
            stripedCount--;

            // �÷��̾� Ÿ���� �������� �ʾҴٸ� Ÿ���� �Ҵ�
            if (!isTypeAssigned && !isFirstTime)
            {
                AssignPlayerType("StripedBall");
            }

            if (!playerType.ContainsKey(playerTurn)) hasExtraTurn = true;
            else if (playerType[playerTurn] == "StripedBall")
            {
                hasExtraTurn = true;
            }
        }
        else if (ball.CompareTag("SolidBall"))
        {
            solidCount--;

            if (!isTypeAssigned && !isFirstTime)
            {
                AssignPlayerType("SolidBall");
            }

            if(!playerType.ContainsKey(playerTurn)) hasExtraTurn = true;
            else if (playerType[playerTurn] == "SolidBall")
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

        OnScoreChanged?.Invoke(solidCount, stripedCount);
    }

    void AssignPlayerType(string ballType)
    {
        isTypeAssigned = true;

        // ���� �÷��̾�� ���ϵ� ���� Ÿ���� �Ҵ�
        playerType[playerTurn] = ballType;

        // ��� �÷��̾� ��ȣ
        int otherPlayer = playerTurn == 1 ? 2 : 1;

        // ��� �÷��̾�� �ٸ� Ÿ�� �Ҵ�
        playerType[otherPlayer] = ballType == "SolidBall" ? "StripedBall" : "SolidBall";

        Debug.Log($"Player {playerTurn} is assigned {playerType[playerTurn]}");
        Debug.Log($"Player {otherPlayer} is assigned {playerType[otherPlayer]}");
    }

    void HandleEightBallPocketed()
    {
        if (!isTypeAssigned)
        {
            // Ÿ���� �������� ���� 8�� ���� ������ ���� �¸�
            winner = playerTurn == 1 ? 2 : 1;
            EndGame();
        }
        else
        {
            // �÷��̾��� ��� ���� �� �־����� Ȯ��
            if ((playerType[playerTurn] == "SolidBall" && solidCount == 0) || (playerType[playerTurn] == "StripedBall" && stripedCount == 0))
            {
                winner = playerTurn;
                EndGame();
            }
            else
            {
                // �ڽ��� ���� �����ִ� ���¿��� 8�� ���� ������ ���� �¸�
                winner = playerTurn == 1 ? 2 : 1;
                EndGame();
            }
        }
    }

    void EndGame()
    {
        Debug.Log($"Player {winner} Wins!");
        DataManager.Instance.WinnerName = winner;
        sceneLoader.ChangeScene("end");
    }

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
        if (cueBallPocketed)
        {
            // ���濡�� ������ �ο�
            freeBall = true;
        }

        if (!hasExtraTurn)
        {
            // �� ����
            playerTurn = playerTurn == 1 ? 2 : 1;
            OnTurnChanged?.Invoke(playerTurn);
        }
        else
        {
            hasExtraTurn = false;
        }

        // ���� ���� ���� ���� �ʱ�ȭ
        pocketedBallsThisTurn.Clear();
        cueBallPocketed = false;
    }

    void CheckIfAllBallsStopped()
    {
        if (!AreAllBallsStopped())
        {
            ballsAreMoving = true;
            return;
        }
        ballsAreMoving = false;

        if (cueController.isHitting)
        {
            cueController.isHitting = false;

            ProcessTurnEnd();
        }
    }

    void ProcessTurnEnd()
    {

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
            if(!isFirstTime) AssignPlayerTypeBasedOnPocketedBalls();
        }
        else
        {
            // �ڽ��� ���� �ƴ� ���� �������� ���
            if (pocketedBallsThisTurn.Any(ball => ball.CompareTag(playerType[playerTurn == 1 ? 2 : 1])))
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

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 170, 250, 20), $"Ball Count - Solid: {solidCount}, Striped: {stripedCount}");
        GUI.Label(new Rect(10, 190, 250, 20), $"Player {playerTurn}'s Turn");
        GUI.Label(new Rect(10, 210, 250, 20), $"Player 1 Type: {(playerType.ContainsKey(1) ? playerType[1] : "Not Assigned")}");
        GUI.Label(new Rect(10, 230, 250, 20), $"Player 2 Type: {(playerType.ContainsKey(2) ? playerType[2] : "Not Assigned")}");
        GUI.Label(new Rect(10, 250, 250, 20), $"isFirst: {isFirstTime}");
    }
}
