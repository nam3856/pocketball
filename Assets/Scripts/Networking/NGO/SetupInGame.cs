using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace LobbyRelaySample.ngo
{
    public class SetupInGame : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_IngameRunnerPrefab = default;
        [SerializeField]
        private GameObject[] m_disableWhileInGame = default;

        private InGameRunner m_inGameRunner;

        private bool m_doesNeedCleanup = false;
        private bool m_hasConnectedViaNGO = false;

        private LocalLobby m_lobby;

        private void SetMenuVisibility(bool areVisible)
        {
            foreach (GameObject go in m_disableWhileInGame)
                go.SetActive(areVisible);
        }

        /// <summary>
        /// Starts the networked game by setting up Relay and NetworkManager.
        /// </summary>
        public void StartNetworkedGame(LocalLobby localLobby, LocalPlayer localPlayer)
        {
            m_doesNeedCleanup = true;
            SetMenuVisibility(false);
            StartCoroutine(CreateNetworkManagerCoroutine(localLobby, localPlayer));
        }

        /// <summary>
        /// Coroutine to handle asynchronous network manager creation.
        /// </summary>
        /// <param name="localLobby">Local lobby instance.</param>
        /// <param name="localPlayer">Local player instance.</param>
        /// <returns>IEnumerator for coroutine.</returns>
        private IEnumerator CreateNetworkManagerCoroutine(LocalLobby localLobby, LocalPlayer localPlayer)
        {
            var networkManagerTask = CreateNetworkManager(localLobby, localPlayer);
            while (!networkManagerTask.IsCompleted)
            {
                yield return null;
            }

            if (networkManagerTask.IsFaulted)
            {
                Debug.LogError("Relay 설정 중 예외 발생: " + networkManagerTask.Exception.GetBaseException().Message);
                yield break;
            }

            yield return null;
        }

        /// <summary>
        /// Initializes the NetworkManager with Relay settings.
        /// </summary>
        /// <param name="localLobby">Local lobby instance.</param>
        /// <param name="localPlayer">Local player instance.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        private async Task CreateNetworkManager(LocalLobby localLobby, LocalPlayer localPlayer)
        {
            m_lobby = localLobby;
            m_inGameRunner = Instantiate(m_IngameRunnerPrefab).GetComponentInChildren<InGameRunner>();
            m_inGameRunner.Initialize(OnConnectionVerified, m_lobby.PlayerCount, OnGameBegin, OnGameEnd, localPlayer);

            if (localPlayer.IsHost.Value)
            {
                string joinCode = await SetRelayHostData(m_lobby.MaxPlayerCount.Value);
                if (!string.IsNullOrEmpty(joinCode))
                {
                    NetworkManager.Singleton.StartHost();
                    Debug.Log("호스트로 게임을 시작합니다. 조인 코드: " + joinCode);
                }
                else
                {
                    Debug.LogError("호스트로 게임을 시작하는 데 실패했습니다.");
                }
            }
            else
            {
                string joinCode = m_lobby.RelayCode.Value;
                if (!string.IsNullOrEmpty(joinCode))
                {
                    bool clientStarted = await SetRelayClientData(joinCode);
                    if (clientStarted)
                    {
                        NetworkManager.Singleton.StartClient();
                        Debug.Log("클라이언트로 게임에 접속합니다. 조인 코드: " + joinCode);
                    }
                    else
                    {
                        Debug.LogError("클라이언트로 게임에 접속하는 데 실패했습니다.");
                    }
                }
                else
                {
                    Debug.LogError("조인 코드가 유효하지 않습니다.");
                }
            }
        }

        /// <summary>
        /// Sets up Relay server data for the host.
        /// </summary>
        /// <param name="maxConnections">Maximum number of connections.</param>
        /// <returns>Join code as a string if successful, null otherwise.</returns>
        public async Task<string> SetRelayHostData(int maxConnections = 2)
        {
            try
            {
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log("Relay 호스트 설정 완료. 조인 코드: " + joinCode);

                return !string.IsNullOrEmpty(joinCode) ? joinCode : null;
            }
            catch (RelayServiceException ex)
            {
                Debug.LogError($"Relay Host Data 설정 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets up Relay server data for the client.
        /// </summary>
        /// <param name="joinCode">Join code obtained from the host.</param>
        /// <returns>True if client started successfully, false otherwise.</returns>
        public async Task<bool> SetRelayClientData(string joinCode)
        {
            try
            {
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
                RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                Debug.Log("Relay 클라이언트 설정 완료.");

                return NetworkManager.Singleton.StartClient();
            }
            catch (RelayServiceException ex)
            {
                Debug.LogError($"Relay Client Data 설정 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Callback when connection is verified.
        /// </summary>
        private void OnConnectionVerified()
        {
            m_hasConnectedViaNGO = true;
        }

        /// <summary>
        /// Called when the game begins.
        /// </summary>
        public void OnGameBegin()
        {
            if (!m_hasConnectedViaNGO)
            {
                LogHandlerSettings.Instance.SpawnErrorPopup("Failed to join the game.");
                OnGameEnd();
            }
        }

        /// <summary>
        /// Cleans up after the game ends.
        /// </summary>
        public void OnGameEnd()
        {
            if (m_doesNeedCleanup)
            {
                NetworkManager.Singleton.Shutdown(true);
                Destroy(m_inGameRunner.transform.parent.gameObject);
                SetMenuVisibility(true);
                m_lobby.RelayCode.Value = "";
                MultiplayManager.Instance.EndGame();
                m_doesNeedCleanup = false;
            }
        }
    }
}
