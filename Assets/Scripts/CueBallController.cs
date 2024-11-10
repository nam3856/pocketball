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

    private float tableMinX = -7.5f;
    private float tableMaxX = 7.5f;
    private float tableMinZ = -3.3f;
    private float tableMaxZ = 3.3f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void HitBall(Vector3 direction, float power, Vector2 hitPoint)
    {
        if (!IsServer)
            return;
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

    public async UniTaskVoid StartFreeBallPlacement(int player)
    {
        cueBallRigidbody.constraints = RigidbodyConstraints.None;
        if (GameManager.Instance.GetMyPlayerNumber() != player) return;
        if (!GameManager.Instance.freeBall.Value)
        {
            return;
        }

        while (GameManager.Instance.freeBall.Value)
        {
            // ���콺 ��ġ�� ������
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.y = transform.position.y;

            // ���콺 ��ġ�� ���̺� ���� ���� ����
            float xPos = Mathf.Clamp(mousePos.x, tableMinX, tableMaxX);
            float zPos = Mathf.Clamp(mousePos.z, tableMinZ, tableMaxZ);
            Vector3 newPosition = new Vector3(xPos, 0.3f, zPos);

            // �ش� ��ġ�� �ٸ� ���� �ִ��� �˻� (Ŭ���̾�Ʈ���� �ӽ÷� �˻�)
            if (IsPositionValid(newPosition))
            {
                transform.position = newPosition; // ť�� ��ġ ������Ʈ
            }

            // ���콺 ���� ��ư Ŭ�� �� ��ġ Ȯ��
            if (Input.GetMouseButtonDown(0) && IsPositionValid(newPosition))
            {
                // ������ ť�� ��ġ ���� ��û
                RequestFreeBallPlacementServerRpc(newPosition);
                // freeBall ���� ������ ������ ��û
                GameManager.Instance.SetFreeBallServerRpc(false);
                cueBallRigidbody.constraints = RigidbodyConstraints.FreezePosition;
                break;
            }

            await UniTask.Yield();
        }
    }

    bool IsPositionValidOnServer(Vector3 position)
    {
        float radius = 0.32f;
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject != transform.gameObject && (collider.gameObject.CompareTag("SolidBall") || collider.gameObject.CompareTag("StripedBall") || collider.gameObject.CompareTag("EightBall")))
            {
                return false;
            }
        }
        return true;
    }

    bool IsPositionValid(Vector3 position)
    {
        float radius = 0.32f; // ���� ������ (�ʿ信 ���� ����)
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject != transform.gameObject && (collider.gameObject.CompareTag("SolidBall") || collider.gameObject.CompareTag("StripedBall") || collider.gameObject.CompareTag("EightBall")))
            {
                return false;
            }
        }
        return true;
    }

    [ServerRpc]
    void RequestFreeBallPlacementServerRpc(Vector3 newPosition)
    {
        // �ش� ��ġ�� �ٸ� ���� �ִ��� �������� �˻�
        if (!IsPositionValidOnServer(newPosition))
            return;

        // ť���� ��ġ�� �������� ����
        transform.position = newPosition;
    }
}
