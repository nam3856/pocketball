using Cysharp.Threading.Tasks;
using UnityEngine;

public class RandomBGMPlayer : MonoBehaviour
{
    public AudioSource audioSource;  // ��������� ����� AudioSource
    public AudioClip[] bgmClips;     // ����� ������� Ŭ�� �迭

    private void Start()
    {
        WaitUntilStart().Forget();
    }

    async UniTaskVoid WaitUntilStart()
    {
        if (GameManager.Instance != null)
        {
            await UniTask.WaitUntil(() => GameManager.Instance.playerTurn.Value >= 1);
            StartMusic();
        }
        else
        {
            Debug.LogError("GameManager �ν��Ͻ��� ã�� �� �����ϴ�.");
        }
    }
    public void StartMusic()
    {
        if (bgmClips.Length > 0)
        {
            PlayRandomBGM();
        }
    }

    void Update()
    {
        // ���� ��� ���� ������ �����ٸ� ���ο� ���� ���� ���
        if (!audioSource.isPlaying)
        {
            PlayRandomBGM();
        }
    }

    void PlayRandomBGM()
    {
        int randomIndex = Random.Range(0, bgmClips.Length); // ���� �ε��� ����
        audioSource.clip = bgmClips[randomIndex];            // ���õ� ���� ����
        audioSource.Play();                                  // ���� ���
    }
}
