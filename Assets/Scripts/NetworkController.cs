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
                Debug.LogWarning("ȣ��Ʈ ���� ����");
                if (stateText != null) SetStateText("�� ����� ����...\n������� �ִ� �� ã�� ��...");
                NetworkManager.Singleton.StartClient();
            }
            else
            {
                if (stateText != null) SetStateText("�ٸ� �÷��̾� ������ ��ٸ��� ��...");
            }
        }
        // Unity ������ ȯ�濡�� ����Ǵ� �ڵ�
        else
        {
            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogWarning("Ŭ���̾�Ʈ ���� ����");
                if (stateText != null) SetStateText("�� ���� ����...\n���ο� �� ����� ��...");
                if (!NetworkManager.Singleton.StartHost())
                {
                    Debug.LogWarning("ȣ��Ʈ ���� ����");
                }
                else
                {
                }
            }
            else
            {
                Debug.Log("Ŭ���̾�Ʈ ���� �õ� ��...");
                if (stateText != null) SetStateText("������� �ִ� �� ã�� ��...");
            }
        }
        
    }
    
}
