using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSettings : NetworkBehaviour
{
    public static GameSettings Instance { get; private set; }

    public NetworkList<ulong> players;
    public string PlayerName { get; private set; }

    public Dictionary<ulong, string> serverPlayerNames = new Dictionary<ulong, string>();
    public Dictionary<ulong, string> clientPlayerNames = new Dictionary<ulong, string>();
    public bool StartAsHost {  get; private set; }
    public bool AllLoaded { get; private set; }
    private Scene m_LoadedScene;

    private void Awake()
    {
        players = new NetworkList<ulong>();
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            if (!NetworkObject.IsSpawned)
            {
                NetworkObject.Spawn();
            }
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

    }
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"클라이언트 {clientId}가 연결을 끊었습니다.");
        RemovePlayerNameClientRpc(clientId);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!players.Contains(clientId))
        {
            if(IsServer) players.Add(clientId);
            Debug.Log($"플레이어 {clientId}가 게임에 참여했습니다.");
            foreach (var kvp in serverPlayerNames)
            {
                UpdatePlayerNameClientRpc(kvp.Key, kvp.Value, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new List<ulong> { clientId }
                    }
                });
            }

            if(serverPlayerNames.ContainsKey(clientId))
            {
                FindAnyObjectByType<TextMeshProUGUI>().text = $"{serverPlayerNames[clientId]}님이 게임에 참여했습니다.";
            }

            // 최소 플레이어 수에 도달하면 게임 시작
            if (players.Count >= 2)
            {
                NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
                AllLoaded = false;
                FindAnyObjectByType<TextMeshProUGUI>().text = $"잠시 후 게임이 시작됩니다...";
                var status = NetworkManager.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
                CheckStatus(status);
            }
        }
    }
    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        var clientOrServer = sceneEvent.ClientId == NetworkManager.ServerClientId ? "server" : "client";
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.LoadComplete:
                {
                    // We want to handle this for only the server-side
                    if (sceneEvent.ClientId == NetworkManager.ServerClientId)
                    {
                        // *** IMPORTANT ***
                        // Keep track of the loaded scene, you need this to unload it
                        m_LoadedScene = sceneEvent.Scene;
                    }
                    Debug.Log($"Loaded the {sceneEvent.SceneName} scene on " +
                        $"{clientOrServer}-({sceneEvent.ClientId}).");
                    break;
                }
            case SceneEventType.UnloadComplete:
                {
                    Debug.Log($"Unloaded the {sceneEvent.SceneName} scene on " +
                        $"{clientOrServer}-({sceneEvent.ClientId}).");
                    break;
                }
            case SceneEventType.LoadEventCompleted:
            case SceneEventType.UnloadEventCompleted:
                {
                    var loadUnload = sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted ? "Load" : "Unload";
                    Debug.Log($"{loadUnload} event completed for the following client " +
                        $"identifiers:({sceneEvent.ClientsThatCompleted})");
                    AllLoaded = true;
                    if (sceneEvent.ClientsThatTimedOut.Count > 0)
                    {
                        Debug.LogWarning($"{loadUnload} event timed out for the following client " +
                            $"identifiers:({sceneEvent.ClientsThatTimedOut})");
                    }
                    break;
                }
        }
    }

    public bool SceneIsLoaded
    {
        get
        {
            if (m_LoadedScene.IsValid() && m_LoadedScene.isLoaded)
            {
                return true;
            }
            return false;
        }
    }

    private void CheckStatus(SceneEventProgressStatus status, bool isLoading = true)
    {
        var sceneEventAction = isLoading ? "load" : "unload";
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"Failed to {sceneEventAction} GameScene with" +
                $" a {nameof(SceneEventProgressStatus)}: {status}");
        }
    }

    public void UnloadScene()
    {
        // Assure only the server calls this when the NetworkObject is
        // spawned and the scene is loaded.
        if (!IsServer || !IsSpawned || !m_LoadedScene.IsValid() || !m_LoadedScene.isLoaded)
        {
            return;
        }

        // Unload the scene
        var status = NetworkManager.SceneManager.UnloadScene(m_LoadedScene);
        CheckStatus(status, false);
    }
    [ClientRpc]
    internal void UpdatePlayerNameClientRpc(ulong clientId, string playerName, ClientRpcParams clientRpcParams = default)
    {
        clientPlayerNames[clientId] = playerName;

        if(!IsHost)
        {
            FindAnyObjectByType<TextMeshProUGUI>().text = $"방을 찾았습니다!";
            StartCoroutine(SetText());
        }
    }

    private IEnumerator SetText()
    {
        yield return new WaitForSeconds(3f);
        FindAnyObjectByType<TextMeshProUGUI>().text = $"잠시 후 게임이 시작됩니다...";
    }
    [ClientRpc]
    private void RemovePlayerNameClientRpc(ulong clientId)
    {
        if (clientPlayerNames.ContainsKey(clientId))
        {
            clientPlayerNames.Remove(clientId);
        }
    }
    public void SetPlayerName(string name)
    {
        PlayerName = name;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerNameServerRpc(ulong clientId, string playerName)
    {
        if (IsServer)
        {
            // 서버의 플레이어 이름 딕셔너리에 추가 또는 업데이트
            serverPlayerNames[clientId] = playerName;

            // 모든 클라이언트에게 플레이어 이름 업데이트
            UpdatePlayerNameClientRpc(clientId, playerName);
        }
    }

    public void SetHostClient(bool p)
    {
        StartAsHost = p;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.SceneManager.OnSceneEvent -= SceneManager_OnSceneEvent;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }
}
