using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StartMenu : MonoBehaviour
{
    public TMP_InputField nicknameInputField;
    public Button hostStartButton;
    public Button clientStartButton;

    void Start()
    {
        hostStartButton.onClick.AddListener(OnHostStartButtonClicked);
        clientStartButton.onClick.AddListener(OnClientStartButtonClicked);
        Screen.SetResolution(1280, 720, false);
    }

    void OnHostStartButtonClicked()
    {
        string nickname = nicknameInputField.text.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("�г����� ��� �ֽ��ϴ�!");
            // ����ڿ��� ��� �޽��� ǥ�� (�ɼ�)
            return;
        }

        // GameSettings�� �г��� ����
        GameSettings.Instance.SetPlayerName(nickname);
        GameSettings.Instance.SetHostClient(true);
        // ���� �� �ε�
        SceneManager.LoadScene("Loading");
    }
    void OnClientStartButtonClicked()
    {
        string nickname = nicknameInputField.text.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("�г����� ��� �ֽ��ϴ�!");
            // ����ڿ��� ��� �޽��� ǥ�� (�ɼ�)
            return;
        }

        // GameSettings�� �г��� ����
        GameSettings.Instance.SetPlayerName(nickname);
        GameSettings.Instance.SetHostClient(false);

        // ���� �� �ε�
        SceneManager.LoadScene("Loading");
    }
}
