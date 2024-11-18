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
        // ���� ���� ����ȭ
        direction = -direction.normalized;

        // ť�밡 �ٶ󺸴� �������� ���� ���� (���� �)
        cueBallRigidbody.AddForce(direction * power, ForceMode.Impulse);

        // Ÿ������ ������ ��� ȸ����(��ũ) ����
        if (hitPoint != Vector2.zero)
        {
            // ť���� ������
            float radius = GetComponent<SphereCollider>().radius;

            // Ÿ������ 3D ���ͷ� ��ȯ (z���� 0)
            Vector3 hitPoint3D = new Vector3(hitPoint.x, hitPoint.y, 0f) * radius;

            // ȸ���� ��� (Ÿ���� ���Ϳ� ���� ���� ������ ����)
            Vector3 torqueAxis = Vector3.Cross(hitPoint3D, direction).normalized;

            // ��ũ�� ũ�� ���
            float torqueMagnitude = power * torqueMultiplier;
            Debug.Log(torqueMagnitude);
            // ȸ����(��ũ) ����
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
