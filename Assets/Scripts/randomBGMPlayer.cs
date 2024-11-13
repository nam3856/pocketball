using UnityEngine;

public class RandomBGMPlayer : MonoBehaviour
{
    public AudioSource audioSource;  // ��������� ����� AudioSource
    public AudioClip[] bgmClips;     // ����� ������� Ŭ�� �迭

    void Start()
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
