using UnityEngine;

public class RandomBGMPlayer : MonoBehaviour
{
    public AudioSource audioSource;  // 배경음악을 재생할 AudioSource
    public AudioClip[] bgmClips;     // 재생할 배경음악 클립 배열

    void Start()
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
