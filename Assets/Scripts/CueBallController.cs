using UnityEngine;
using Unity.Netcode;
public class CueBallController : NetworkBehaviour
{
    public AudioSource audioSource;
    public Rigidbody CueBallRigidbody;
    public CueController cueController;
    public float torqueMultiplier = 20f;
    public AudioClip hitAudioClip;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void HitBall(Vector3 direction, float power, Vector2 hitPoint)
    {
        if (!IsServer)
            return;
        // 방향 벡터 정규화
        direction = direction.normalized;

        // 큐대가 바라보는 방향으로 힘을 적용 (선형 운동)
        CueBallRigidbody.AddForce(direction * power, ForceMode.Impulse);

        // 타격점이 설정된 경우 회전력(토크) 적용
        if (hitPoint != Vector2.zero)
        {
            // 큐볼의 반지름
            float radius = GetComponent<SphereCollider>().radius;

            // 타격점을 3D 벡터로 변환 (z축은 0)
            Vector3 hitPoint3D = new Vector3(hitPoint.x, hitPoint.y, 0f) * radius;

            // 회전축 계산 (타격점 벡터와 힘의 방향 벡터의 외적)
            Vector3 torqueAxis = Vector3.Cross(hitPoint3D, direction).normalized;

            // 토크의 크기 계산
            float torqueMagnitude = power * torqueMultiplier;
            Debug.Log(torqueMagnitude);
            // 회전력(토크) 적용
            CueBallRigidbody.AddTorque(torqueAxis * torqueMagnitude, ForceMode.Impulse);

            PlayCueBallHitAudioClientRpc();
        }
    }
    [ClientRpc]
    void PlayCueBallHitAudioClientRpc()
    {
        if (audioSource != null)
        {
            audioSource.PlayOneShot(hitAudioClip);
        }
    }
    public bool IsBallStopped()
    {
        if (CueBallRigidbody.angularVelocity.magnitude < 1f && CueBallRigidbody.velocity.magnitude < 0.1f) CueBallRigidbody.angularVelocity = Vector2.zero;
        return CueBallRigidbody.velocity.magnitude < 0.1f && CueBallRigidbody.angularVelocity.magnitude < 0.1f;
    }
}
