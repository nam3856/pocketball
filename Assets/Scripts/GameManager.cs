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
        // 현재 턴에 포켓된 공들을 저장
        pocketedBallsThisTurn.Add(ball);

        if (ball.CompareTag("StripedBall"))
        {
            stripedCount--;

            // 플레이어 타입이 정해지지 않았다면 타입을 할당
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
            // 8번 공 처리 로직
            HandleEightBallPocketed();
        }

        OnScoreChanged?.Invoke(solidCount, stripedCount);
    }

    void AssignPlayerType(string ballType)
    {
        isTypeAssigned = true;

        // 현재 플레이어에게 포켓된 공의 타입을 할당
        playerType[playerTurn] = ballType;

        // 상대 플레이어 번호
        int otherPlayer = playerTurn == 1 ? 2 : 1;

        // 상대 플레이어에게 다른 타입 할당
        playerType[otherPlayer] = ballType == "SolidBall" ? "StripedBall" : "SolidBall";

        Debug.Log($"Player {playerTurn} is assigned {playerType[playerTurn]}");
        Debug.Log($"Player {otherPlayer} is assigned {playerType[otherPlayer]}");
    }

    void HandleEightBallPocketed()
    {
        if (!isTypeAssigned)
        {
            // 타입이 정해지기 전에 8번 공을 넣으면 상대방 승리
            winner = playerTurn == 1 ? 2 : 1;
            EndGame();
        }
        else
        {
            // 플레이어의 모든 공을 다 넣었는지 확인
            if ((playerType[playerTurn] == "SolidBall" && solidCount == 0) || (playerType[playerTurn] == "StripedBall" && stripedCount == 0))
            {
                winner = playerTurn;
                EndGame();
            }
            else
            {
                // 자신의 공이 남아있는 상태에서 8번 공을 넣으면 상대방 승리
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
            // 상대방에게 프리볼 부여
            freeBall = true;
        }

        if (!hasExtraTurn)
        {
            // 턴 변경
            playerTurn = playerTurn == 1 ? 2 : 1;
            OnTurnChanged?.Invoke(playerTurn);
        }
        else
        {
            hasExtraTurn = false;
        }

        // 다음 턴을 위해 변수 초기화
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
            if(!isFirstTime) AssignPlayerTypeBasedOnPocketedBalls();
        }
        else
        {
            // 자신의 공이 아닌 공을 포켓했을 경우
            if (pocketedBallsThisTurn.Any(ball => ball.CompareTag(playerType[playerTurn == 1 ? 2 : 1])))
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

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 170, 250, 20), $"Ball Count - Solid: {solidCount}, Striped: {stripedCount}");
        GUI.Label(new Rect(10, 190, 250, 20), $"Player {playerTurn}'s Turn");
        GUI.Label(new Rect(10, 210, 250, 20), $"Player 1 Type: {(playerType.ContainsKey(1) ? playerType[1] : "Not Assigned")}");
        GUI.Label(new Rect(10, 230, 250, 20), $"Player 2 Type: {(playerType.ContainsKey(2) ? playerType[2] : "Not Assigned")}");
        GUI.Label(new Rect(10, 250, 250, 20), $"isFirst: {isFirstTime}");
    }
}
