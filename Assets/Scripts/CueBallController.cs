using UnityEngine;
using Unity.Netcode;
using Cysharp.Threading.Tasks;
public class CueBallController : NetworkBehaviour
{
    public AudioSource audioSource;
    public Rigidbody cueBallRigidbody;
    public CueController cueController;
    public float torqueMultiplier = 20f;
    public AudioClip hitAudioClip;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        cueBallRigidbody = GetComponent<Rigidbody>();
    }

    public void HitBall(Vector3 direction, float power, Vector2 hitPoint)
    {
        // 방향 벡터 정규화
        direction = -direction.normalized;

        // 큐대가 바라보는 방향으로 힘을 적용 (선형 운동)
        cueBallRigidbody.AddForce(direction * power, ForceMode.Impulse);

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
            cueBallRigidbody.AddTorque(torqueAxis * torqueMagnitude, ForceMode.Impulse);

        }
        Debug.Log("Cue Ball Hitted");
        PlayCueBallHitAudioClientRpc();
    }
    [ClientRpc]
    void PlayCueBallHitAudioClientRpc()
    {
        if (audioSource != null)
        {
            audioSource.PlayOneShot(hitAudioClip);
        }
    }
    [ServerRpc]
    internal void CompleteBallInHandServerRpc(Vector3 newPosition)
    {
        UpdateCueBallPositionClientRpc(newPosition);
    }
    [ClientRpc]
    private void UpdateCueBallPositionClientRpc(Vector3 position)
    {
        transform.position = new Vector3(position.x,0.3f,position.z);
        cueBallRigidbody.angularVelocity = Vector3.zero;
        cueBallRigidbody.velocity = Vector3.zero;
        cueBallRigidbody.constraints = RigidbodyConstraints.FreezePosition;
    }
}
