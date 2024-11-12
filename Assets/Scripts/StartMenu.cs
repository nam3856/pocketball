using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StartMenu : MonoBehaviour
{
    public TMP_InputField nicknameInputField;
    public Button startButton;

    void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        Screen.SetResolution(1280, 720, false);
    }

    void OnStartButtonClicked()
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

        // ���� �� �ε�
        SceneManager.LoadScene("SampleScene");
    }
}
