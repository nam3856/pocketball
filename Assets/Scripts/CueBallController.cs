using UnityEngine;

public class CueBallController : MonoBehaviour
{
    public AudioSource audioSource;
    public Rigidbody CueBallRigidbody;
    public CueController cueController;
    public float torqueMultiplier = 20f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void HitBall(Vector3 direction, float power, Vector2 hitPoint)
    {
        // ���� ���� ����ȭ
        direction = direction.normalized;

        // ť�밡 �ٶ󺸴� �������� ���� ���� (���� �)
        CueBallRigidbody.AddForce(direction * power, ForceMode.Impulse);

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
            CueBallRigidbody.AddTorque(torqueAxis * torqueMagnitude, ForceMode.Impulse);
        }
    }

    public bool IsBallStopped()
    {
        if (CueBallRigidbody.angularVelocity.magnitude < 1f && CueBallRigidbody.velocity.magnitude < 0.1f) CueBallRigidbody.angularVelocity = Vector2.zero;
        return CueBallRigidbody.velocity.magnitude < 0.1f && CueBallRigidbody.angularVelocity.magnitude < 0.1f;
    }

    public void Respawn()
    {
        transform.position = new Vector3 (-4.5f,0.5f);

        CueBallRigidbody.velocity = Vector3.zero;
        CueBallRigidbody.angularVelocity = Vector3.zero;
    }

    /*
    void OnGUI()
	{
        GUI.Label(new Rect(10, 70, 250, 20), $"Cue ball velocity: {CueBallRigidbody.velocity.magnitude}");
    }
    */
}
