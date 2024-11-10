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
            // 마우스 위치를 가져옴
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.y = transform.position.y;

            // 마우스 위치를 테이블 영역 내로 제한
            float xPos = Mathf.Clamp(mousePos.x, tableMinX, tableMaxX);
            float zPos = Mathf.Clamp(mousePos.z, tableMinZ, tableMaxZ);
            Vector3 newPosition = new Vector3(xPos, 0.3f, zPos);

            // 해당 위치에 다른 공이 있는지 검사 (클라이언트에서 임시로 검사)
            if (IsPositionValid(newPosition))
            {
                transform.position = newPosition; // 큐볼 위치 업데이트
            }

            // 마우스 왼쪽 버튼 클릭 시 위치 확정
            if (Input.GetMouseButtonDown(0) && IsPositionValid(newPosition))
            {
                // 서버에 큐볼 위치 변경 요청
                RequestFreeBallPlacementServerRpc(newPosition);
                // freeBall 상태 변경을 서버에 요청
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
        float radius = 0.32f; // 공의 반지름 (필요에 따라 조정)
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
        // 해당 위치에 다른 공이 있는지 서버에서 검사
        if (!IsPositionValidOnServer(newPosition))
            return;

        // 큐볼의 위치를 서버에서 변경
        transform.position = newPosition;
    }
}
