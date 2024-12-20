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
            Debug.LogWarning("닉네임이 비어 있습니다!");
            // 사용자에게 경고 메시지 표시 (옵션)
            return;
        }

        // GameSettings에 닉네임 저장
        GameSettings.Instance.SetPlayerName(nickname);
        GameSettings.Instance.SetHostClient(true);
        // 게임 씬 로드
        SceneManager.LoadScene("Loading");
    }
    void OnClientStartButtonClicked()
    {
        string nickname = nicknameInputField.text.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("닉네임이 비어 있습니다!");
            // 사용자에게 경고 메시지 표시 (옵션)
            return;
        }

        // GameSettings에 닉네임 저장
        GameSettings.Instance.SetPlayerName(nickname);
        GameSettings.Instance.SetHostClient(false);

        // 게임 씬 로드
        SceneManager.LoadScene("Loading");
    }
}
