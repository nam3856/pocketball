using Cysharp.Threading.Tasks;
using UnityEngine;

public class RandomBGMPlayer : MonoBehaviour
{
    public AudioSource audioSource;  // 배경음악을 재생할 AudioSource
    public AudioClip[] bgmClips;     // 재생할 배경음악 클립 배열

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
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다.");
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
        // 현재 재생 중인 음악이 끝났다면 새로운 랜덤 음악 재생
        if (!audioSource.isPlaying)
        {
            PlayRandomBGM();
        }
    }

    void PlayRandomBGM()
    {
        int randomIndex = Random.Range(0, bgmClips.Length); // 랜덤 인덱스 선택
        audioSource.clip = bgmClips[randomIndex];            // 선택된 음악 설정
        audioSource.Play();                                  // 음악 재생
    }
}
