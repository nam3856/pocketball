using Unity.Netcode;
using UnityEngine;

public class NetworkController : MonoBehaviour
{
    void Start()
    {
#if !UNITY_EDITOR
        // Unity 에디터 환경에서 실행되는 코드
        if (!NetworkManager.Singleton.StartHost())
        {
            Debug.LogWarning("호스트 시작 실패");

            
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.StartClient();
        }
        else
        {
            Debug.Log("호스트 시작 시도 중...");
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
#else
    // 빌드된 버전에서 실행되는 코드
    if (!NetworkManager.Singleton.StartClient())
    {
        Debug.LogWarning("클라이언트 시작 실패. 호스트로 전환합니다.");
        NetworkManager.Singleton.StartHost();
    }
    else
    {
        Debug.Log("클라이언트 시작 시도 중...");
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }
#endif
    }



    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"클라이언트 {clientId}가 연결을 끊었습니다.");
        // 필요 시 추가 로직
    }
}
