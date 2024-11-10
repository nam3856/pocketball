using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Information")]
    public List<ulong> players = new List<ulong>();
    public NetworkVariable<ulong> player1ClientId = new NetworkVariable<ulong>();
    public NetworkVariable<ulong> player2ClientId = new NetworkVariable<ulong>();
    public NetworkVariable<int> playerTurn = new NetworkVariable<int>(1);
    private NetworkVariable<int> winner = new NetworkVariable<int>(0);
    public NetworkVariable<FixedString32Bytes> player1Type = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<FixedString32Bytes> player2Type = new NetworkVariable<FixedString32Bytes>();

    [Header("Game Objects & Controllers")]
    public GameObject cuePrefab;
    public GameObject cueBallPrefab;
    public GameObject ball1Prefab; // 1번 공
    public GameObject ball8Prefab; // 1번 공
    public GameObject eightBall; // 8번 공
    public GameObject stripedBallPrefab; // 줄무늬 공 프리팹 리스트
    public GameObject solidBallPrefab; // 단색 공 프리팹 리스트
    public CueController cueController;
    private CueBallController cueBallController;
    public SceneLoader sceneLoader;
    private GameObject cue;
    private GameObject cueBall;
    private NetworkObject cueNetworkObject;
    public List<BallController> ballControllers = new List<BallController>();
    public List<NetworkObject> spawnedNetworkBalls = new List<NetworkObject>();

    [Header("Network Variables")]
    public NetworkVariable<int> solidCount = new NetworkVariable<int>(7);
    public NetworkVariable<int> stripedCount = new NetworkVariable<int>(7);
    public NetworkVariable<NetworkObjectReference> cueReference = new NetworkVariable<NetworkObjectReference>();
    public NetworkVariable<NetworkObjectReference> cueBallReference = new NetworkVariable<NetworkObjectReference>();
    public NetworkVariable<bool> hasExtraTurn = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> freeBall = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> ballsAreMoving = new NetworkVariable<bool>(false);

    [Header("Table Boundaries")]
    private Vector3 tableLeftEnd = new Vector3(-7.8f, 0.33f, 0f);
    private Vector3 tableRightEnd = new Vector3(7.8f, 0.33f, 0f);

    [Header("Ball Settings")]
    public float ballRadius = 0.32f;
    public float triangleSpacing = 0.66f;

    // Private Variables
    private bool isTypeAssigned = false;
    private bool isFirstTime = true;
    private List<GameObject> pocketedBallsThisTurn = new List<GameObject>();
    private bool cueBallPocketed = false;
    private CancellationTokenSource movementCheckCancellationTokenSource;
    private List<Material> usedMaterials = new List<Material>();
    private void OnClientConnected(ulong clientId)
    {
        if (!players.Contains(clientId))
        {
            players.Add(clientId);
            Debug.Log($"플레이어 {clientId}가 게임에 참여했습니다.");

            // 최소 플레이어 수에 도달하면 게임 시작
            if (players.Count >= 2)
            {
                StartGame();
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (players.Contains(clientId))
        {
            players.Remove(clientId);
            Debug.Log($"플레이어 {clientId}가 게임에서 떠났습니다.");

            // 플레이어 수가 줄어들면 게임 종료 또는 대기 상태로 전환
        }
    }
    public void StartGame()
    {
        if (IsServer)
        {
            Debug.Log("GameManager: 게임 시작");
            SpawnCue();
            SpawnBalls();
            AssignCueOwnershipServerRpc();
        }
        else
        {
        }
    }
    private void SpawnCue()
    {
        cue = Instantiate(cuePrefab, Vector3.zero, Quaternion.identity);
        cueNetworkObject = cue.GetComponent<NetworkObject>();
        cueNetworkObject.Spawn();
        cueController = cue.GetComponent<CueController>();
        cueReference.Value = new NetworkObjectReference(cueNetworkObject);
        HitPointIndicatorController hitPointIndicator = FindObjectOfType<HitPointIndicatorController>();
        if (hitPointIndicator != null)
        {
            hitPointIndicator.SubscribeEvent(cueController);
        }

    }
    private void SpawnBalls()
    {
        InitializeBalls();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        solidCount.OnValueChanged += OnSolidCountChanged;
        stripedCount.OnValueChanged += OnStripedCountChanged;
        playerTurn.OnValueChanged += OnPlayerTurnChanged;

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        }
        if (IsClient)
        {
            freeBall.OnValueChanged += HandleFreeBallChange;
            ballsAreMoving.OnValueChanged += HandleBallsMovingChange;
            cueReference.OnValueChanged += (previousValue, newValue) =>
            {
                if (newValue.TryGet(out NetworkObject cueNetworkObject))
                {
                    cue = cueNetworkObject.gameObject;
                    cueController = cue.GetComponent<CueController>();
                }
            };
            cueBallReference.OnValueChanged += (previousValue, newValue) =>
            {
                if (newValue.TryGet(out NetworkObject cueBallNetworkObject))
                {
                    cueBall = cueBallNetworkObject.gameObject;
                    cueBallController = cueBall.GetComponent<CueBallController>();
                    cueController.CueBall = cueBall.transform;
                    cueController.cueBallController = cueBallController;
                }
            };
        }
    }

    private void HandleFreeBallChange(bool previousValue, bool newValue)
    {
        if (cueController != null)
        {
            if (newValue)
                cueController.HideCue();
            else
                cueController.ShowCue();
        }
    }

    private void HandleBallsMovingChange(bool previousValue, bool newValue)
    {
        if (cueController != null)
        {
            if (newValue)
                cueController.HideCue();
            else
                cueController.ShowCue();
        }
    }
    private void InitializeBalls()
    {
        // 1. 큐볼 스폰 (왼쪽 끝)
        Vector3 cueBallPosition = CalculateCueBallPosition();
        cueBall = Instantiate(cueBallPrefab, cueBallPosition, Quaternion.identity);
        cueBall.GetComponent<NetworkObject>().Spawn();
        cueBallController = cueBall.GetComponent<CueBallController>();
        cueBallController.cueController = cueController;
        cueController.CueBall = cueBall.transform;
        cueController.cueBallController = cueBallController;
        cueController.hitPointIndicator = GameObject.Find("Canvas/aboutHit/HitIndicator").transform;
        ballControllers.Add(cueBall.GetComponent<BallController>());
        spawnedNetworkBalls.Add(cueBall.GetComponent<NetworkObject>());
        Debug.Log($"CueBall spawned at: {cueBallPosition}");

        // 2. 삼각형 형태로 15개의 공 스폰 (오른쪽 끝)
        SpawnBallTriangle();
    }

    private Vector3 CalculateCueBallPosition()
    {
        // 큐볼을 테이블의 왼쪽 끝에 배치
        Vector3 tableDirection = (tableRightEnd - tableLeftEnd).normalized;
        return tableLeftEnd + tableDirection * 2f;
    }

    private void SpawnBallTriangle()
    {
        Vector3 triangleOrigin = new Vector3(2, 0.33f, 0);
        Vector3 tableDirection = (tableRightEnd - tableLeftEnd).normalized;
        Vector3 perpendicular = Vector3.Cross(tableDirection, Vector3.up).normalized;
        int currentBallIndex = 1;
        int rowCount = 1;
        List<GameObject> spawnedBalls = new List<GameObject>();

        // 1번 줄 - 1번 공
        GameObject num1Ball = Instantiate(ball1Prefab, GetPositionInTriangle(triangleOrigin, tableDirection, perpendicular, rowCount++, 0), Quaternion.identity);

        // 2번 줄 - 줄무늬 랜덤, 단색 랜덤
        SpawnRow(triangleOrigin, tableDirection, perpendicular, rowCount++, new[] { "striped", "solid" }, spawnedBalls);

        // 3번 줄 - 단색 랜덤, 8번 공, 줄무늬 랜덤
        eightBall = Instantiate(ball8Prefab, GetPositionInTriangle(triangleOrigin, tableDirection, perpendicular, rowCount, 1), Quaternion.identity);
        SpawnRow(triangleOrigin, tableDirection, perpendicular, rowCount++, new[] { "solid", "striped" }, spawnedBalls, 1);

        // 4번 줄 - 줄무늬 랜덤, 단색 랜덤, 줄무늬 랜덤, 단색 랜덤
        SpawnRow(triangleOrigin, tableDirection, perpendicular, rowCount++, new[] { "striped", "solid", "striped", "solid" }, spawnedBalls);

        // 5번 줄 - 줄무늬 랜덤, 단색 랜덤, 줄무늬 랜덤, 단색 랜덤, 줄무늬 랜덤
        SpawnRow(triangleOrigin, tableDirection, perpendicular, rowCount, new[] { "striped", "solid", "striped", "solid", "striped" }, spawnedBalls);

        foreach (var ball in spawnedBalls)
        {
            if (!ball.GetComponent<NetworkObject>().IsSpawned)
            {
                ball.GetComponent<NetworkObject>().Spawn();
                spawnedNetworkBalls.Add(ball.GetComponent<NetworkObject>());
            }
            ball.transform.rotation = Quaternion.Euler(90f, 0, 0);
            if (currentBallIndex == 7) currentBallIndex++;

            BallController ballController = ball.GetComponent<BallController>();

            string materialPath = "";
            if (ball.tag == "StripedBall")
            {
                // 줄무늬 공에서 사용되지 않은 머티리얼 선택
                var availableMaterials = Enumerable.Range(9, 7)
                    .Where(i => !usedMaterials.Any(m => m.name == $"Ball{i:D2}"))
                    .ToList();

                if (availableMaterials.Any())
                {
                    int materialIndex = availableMaterials[UnityEngine.Random.Range(0, availableMaterials.Count)];
                    materialPath = $"Materials/Ball{materialIndex:D2}";
                    // 사용된 머티리얼 추가
                    Material material = Resources.Load<Material>(materialPath);
                    if (material != null)
                    {
                        usedMaterials.Add(material);
                    }
                }
            }
            else // Solid Ball
            {
                // 단색 공에서 사용되지 않은 머티리얼 선택
                var availableMaterials = Enumerable.Range(2, 6)
                    .Where(i => !usedMaterials.Any(m => m.name == $"Ball{i:D2}"))
                    .ToList();

                if (availableMaterials.Any())
                {
                    int materialIndex = availableMaterials[UnityEngine.Random.Range(0, availableMaterials.Count)];
                    materialPath = $"Materials/Ball{materialIndex:D2}";
                    // 사용된 머티리얼 추가
                    Material material = Resources.Load<Material>(materialPath);
                    if (material != null)
                    {
                        usedMaterials.Add(material);
                    }
                }
            }

            Debug.Log(materialPath);
            if (!string.IsNullOrEmpty(materialPath))
            {
                NetworkObjectReference networkObjectReference = new NetworkObjectReference(ball.GetComponent<NetworkObject>());
                SetBallMaterialServerRpc(networkObjectReference, materialPath);
            }

            ballController.ballNumber = currentBallIndex + 1;
            ballController.BallRigidbody = ball.GetComponent<Rigidbody>();
            ballControllers.Add(ballController);

            currentBallIndex++;
        }

        // 1번 공과 8번 공은 별도로 스폰
        spawnedBalls.Add(num1Ball);
        spawnedBalls.Add(eightBall);
        num1Ball.GetComponent<NetworkObject>().Spawn();
        num1Ball.transform.rotation = Quaternion.Euler(90f, 0, 0);
        eightBall.GetComponent<NetworkObject>().Spawn();
        eightBall.transform.rotation = Quaternion.Euler(90f, 0, 0);
    }

    private void SpawnRow(Vector3 origin, Vector3 direction, Vector3 perpendicular, int row, string[] types, List<GameObject> spawnedBalls, int skipIndex = -1)
    {
        for (int i = 0; i < types.Length; i++)
        {
            if (i == skipIndex)
            {
                Vector3 position = GetPositionInTriangle(origin, direction, perpendicular, row, i + 1);
                GameObject prefab = GetBallPrefabType(types[i]);
                GameObject ball = Instantiate(prefab, position, Quaternion.identity);
                spawnedBalls.Add(ball);
            }
            else
            {
                Vector3 position = GetPositionInTriangle(origin, direction, perpendicular, row, i);
                GameObject prefab = GetBallPrefabType(types[i]);
                GameObject ball = Instantiate(prefab, position, Quaternion.identity);
                spawnedBalls.Add(ball);
            }
        }
    }

    private Vector3 GetPositionInTriangle(Vector3 origin, Vector3 direction, Vector3 perpendicular, int row, int index)
    {
        // 각 줄의 시작 위치를 계산하고 공의 위치를 좌우로 퍼지게 배치
        Vector3 rowStart = origin + direction * (row - 1) * triangleSpacing * Mathf.Sin(60 * Mathf.Deg2Rad);
        return rowStart + perpendicular * ((index - (row - 1) / 2.0f) * triangleSpacing);
    }

    private GameObject GetBallPrefabType(string type)
    {
        if (type == "striped")
        {
            return stripedBallPrefab;
        }
        else
        {
            return solidBallPrefab;
        }
    }

    [ServerRpc]
    public void SetBallMaterialServerRpc(NetworkObjectReference ballReference, string materialName)
    {
        if (ballReference.TryGet(out NetworkObject networkObject))
        {
            GameObject ball = networkObject.gameObject;
            Material material = Resources.Load<Material>(materialName);
            if (material != null)
            {
                // 머티리얼 설정
                ball.GetComponent<MeshRenderer>().material = material;

                // 모든 클라이언트에게 머티리얼 설정 전달
                SetBallMaterialClientRpc(ballReference, materialName);
            }
            else
            {
                Debug.LogWarning($"Material {materialName} not found.");
            }
        }
    }

    [ClientRpc]
    private void SetBallMaterialClientRpc(NetworkObjectReference ballReference, string materialName)
    {
        if (ballReference.TryGet(out NetworkObject networkObject))
        {
            GameObject ball = networkObject.gameObject;
            Material material = Resources.Load<Material>(materialName);
            if (material != null)
            {
                ball.GetComponent<MeshRenderer>().material = material;
            }
        }
    }


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
        isFirstTime = true;

        if (IsServer)
        {
            solidCount.Value = 7;
            stripedCount.Value = 7;
            playerTurn.Value = 1;
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
        if (freeBall.Value)
        {
            cueBallController.StartFreeBallPlacement(newValue).Forget();
        }
        cueController.StartCueControlAsync().Forget();
    }

    public int GetMyPlayerNumber()
    {
        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        if (player1ClientId.Value == myClientId)
            return 2;
        else if (player2ClientId.Value == myClientId)
            return 1;
        else
            return 0; // 할당되지 않음
    }

    string GetPlayerType(int playerId)
    {
        if (playerId == 1)
        {
            return player1Type.Value.ToString();
        }
        else if (playerId == 2)
        {
            return player2Type.Value.ToString();
        }
        else
        {
            return null;
        }
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
            ball.BallRigidbody.constraints = RigidbodyConstraints.FreezePosition;
        }
        return true;
    }
    [ServerRpc]
    public void HitConfirmedServerRpc()
    {
        foreach (var ball in spawnedNetworkBalls)
        {
            NetworkObjectReference ballReference = new NetworkObjectReference(ball);
            SetRigidbodyConstraintsServerRpc(ballReference);
        }
    }

    [ServerRpc]
    public void SetRigidbodyConstraintsServerRpc(NetworkObjectReference objectReference)
    {
        if (objectReference.TryGet(out NetworkObject networkObject))
        {
            Rigidbody rb = networkObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.None;

                SetRigidbodyConstraintsClientRpc(objectReference);
            }
        }
    }
    [ClientRpc]
    public void SetRigidbodyConstraintsClientRpc(NetworkObjectReference objectReference)
    {
        if (objectReference.TryGet(out NetworkObject networkObject))
        {
            Rigidbody rb = networkObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.None;
            }
        }
    }

    public async UniTaskVoid CheckBallsMovementAsync()
    {
        if (!IsServer) return; // 서버에서만 실행

        // 이전에 동작 중인 움직임 검사 취소
        movementCheckCancellationTokenSource?.Cancel();
        movementCheckCancellationTokenSource = new CancellationTokenSource();

        // 공이 움직이고 있음을 표시
        ballsAreMoving.Value = true;
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

        // 모든 공이 멈출 때까지 검사
        while (!AreAllBallsStopped())
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: movementCheckCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        // 공이 모두 멈췄을 때 처리
        ballsAreMoving.Value = false;

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

            if (!IsPlayerTypeAssigned(playerTurn.Value)) hasExtraTurn.Value = true;
            else if (GetPlayerType(playerTurn.Value) == "StripedBall")
            {
                hasExtraTurn.Value = true;
            }
        }
        else if (ball.CompareTag("SolidBall"))
        {
            solidCount.Value--;

            if (!isTypeAssigned && !isFirstTime)
            {
                AssignPlayerType("SolidBall");
            }

            if (!IsPlayerTypeAssigned(playerTurn.Value)) hasExtraTurn.Value = true;
            else if (GetPlayerType(playerTurn.Value) == "SolidBall")
            {
                hasExtraTurn.Value = true;
            }
        }
        else if (ball.CompareTag("CueBall"))
        {
            cueBallPocketed = true;
            SetFreeBallServerRpc(true);
        }
        else if (ball == eightBall)
        {
            // 8번 공 처리 로직
            HandleEightBallPocketed();
        }
    }

    bool IsPlayerTypeAssigned(int playerId)
    {
        var type = GetPlayerType(playerId);
        return !string.IsNullOrEmpty(type);
    }

    void AssignPlayerType(string ballType)
    {
        isTypeAssigned = true;

        int currentPlayer = playerTurn.Value;
        int otherPlayer = currentPlayer == 1 ? 2 : 1;
        FixedString32Bytes playerBallType = ballType;
        FixedString32Bytes otherBallType = ballType == "SolidBall" ? "StripedBall" : "SolidBall";

        if (currentPlayer == 1)
        {
            player1Type.Value = playerBallType;
            player2Type.Value = otherBallType;
        }
        else
        {
            player2Type.Value = playerBallType;
            player1Type.Value = otherBallType;
        }

        Debug.Log($"Player {currentPlayer} is assigned {playerBallType}");
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
        sceneLoader.ChangeScene("end");
    }

    void TurnChange()
    {
        if (cueBallPocketed)
        {
            SetFreeBallServerRpc(true);
        }

        if (!hasExtraTurn.Value)
        {
            // 턴 변경
            playerTurn.Value = playerTurn.Value == 1 ? 2 : 1;
            AssignCueOwnershipServerRpc();
            NotifyTurnChangedClientRpc(playerTurn.Value);
        }
        else
        {
            hasExtraTurn.Value = false;
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
            hasExtraTurn.Value = false; // 추가 턴 없음
            TurnChange();
            return;
        }

        // 포켓된 공이 없는 경우
        if (pocketedBallsThisTurn.Count == 0)
        {
            isFirstTime = false;
            hasExtraTurn.Value = false;
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
                hasExtraTurn.Value = false;
                TurnChange();
                return;
            }
        }

        isFirstTime = false;
        // 추가 턴 여부에 따라 턴 변경
        if (!hasExtraTurn.Value)
        {
            TurnChange();
        }
        else
        {
            hasExtraTurn.Value = false;
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
            hasExtraTurn.Value = true;
        }
        else if (stripedPocketed > 0 && solidPocketed == 0)
        {
            AssignPlayerType("StripedBall");
            hasExtraTurn.Value = true;
        }
        else
        {
            // 둘 다 넣었거나 아무것도 넣지 않았을 경우 턴 변경
            hasExtraTurn.Value = false;
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

    //private void OnGUI()
    //{
    //    GUI.Label(new Rect(10, 170, 250, 20), $"Ball Count - Solid: {solidCount.Value}, Striped: {stripedCount.Value}");
    //    GUI.Label(new Rect(10, 190, 250, 20), $"Player {playerTurn.Value}'s Turn");
    //    GUI.Label(new Rect(10, 210, 250, 20), $"Player 1 Type: {GetPlayerType(1) ?? "Not Assigned"}");
    //    GUI.Label(new Rect(10, 230, 250, 20), $"Player 2 Type: {GetPlayerType(2) ?? "Not Assigned"}");
    //    GUI.Label(new Rect(10, 250, 250, 20), $"isFirst: {isFirstTime}");
    //}

    [ServerRpc]
    public void SetFreeBallServerRpc(bool value)
    {
        freeBall.Value = value;
    }

    [ServerRpc]
    public void SethasExtraTurnServerRpc(bool v)
    {
        hasExtraTurn.Value = v;
    }

    [ServerRpc]
    private void AssignCueOwnershipServerRpc()
    {
        if (players.Count == 0)
        {
            Debug.LogWarning("No players connected.");
            return;
        }

        ulong currentPlayerId = players[playerTurn.Value-1];
        cueNetworkObject.ChangeOwnership(currentPlayerId);
        Debug.Log($"Cue ownership changed to player {currentPlayerId}");
    }
}
