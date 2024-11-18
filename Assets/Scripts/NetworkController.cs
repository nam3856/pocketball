using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkController : MonoBehaviour
{
    public TextMeshProUGUI stateText;

    public void SetStateText(string text)
    {
        stateText.text = text;
    }
    void Start()
    {
        if (GameSettings.Instance.StartAsHost)
        {
            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogWarning("호스트 시작 실패");
                if (stateText != null) SetStateText("방 만들기 실패...\n만들어져 있는 방 찾는 중...");
                NetworkManager.Singleton.StartClient();
            }
            else
            {
                if (stateText != null) SetStateText("다른 플레이어 참가를 기다리는 중...");
            }
        }
        // Unity 에디터 환경에서 실행되는 코드
        else
        {
            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogWarning("클라이언트 시작 실패");
                if (stateText != null) SetStateText("방 들어가기 실패...\n새로운 방 만드는 중...");
                if (!NetworkManager.Singleton.StartHost())
                {
                    Debug.LogWarning("호스트 시작 실패");
                }
                else
                {
                }
            }
            else
            {
                Debug.Log("클라이언트 시작 시도 중...");
                if (stateText != null) SetStateText("만들어져 있는 방 찾는 중...");
            }
        }
        
    }
    
}
