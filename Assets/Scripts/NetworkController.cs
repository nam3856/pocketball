using Unity.Netcode;
using UnityEngine;

public class NetworkController : MonoBehaviour
{
    void Start()
    {
#if !UNITY_EDITOR
        // Unity ������ ȯ�濡�� ����Ǵ� �ڵ�
        if (!NetworkManager.Singleton.StartHost())
        {
            Debug.LogWarning("ȣ��Ʈ ���� ����");

            
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.StartClient();
        }
        else
        {
            Debug.Log("ȣ��Ʈ ���� �õ� ��...");
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
#else
    // ����� �������� ����Ǵ� �ڵ�
    if (!NetworkManager.Singleton.StartClient())
    {
        Debug.LogWarning("Ŭ���̾�Ʈ ���� ����. ȣ��Ʈ�� ��ȯ�մϴ�.");
        NetworkManager.Singleton.StartHost();
    }
    else
    {
        Debug.Log("Ŭ���̾�Ʈ ���� �õ� ��...");
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }
#endif
    }



    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Ŭ���̾�Ʈ {clientId}�� ������ �������ϴ�.");
        // �ʿ� �� �߰� ����
    }
}
