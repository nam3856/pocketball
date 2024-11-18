using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class BallController : NetworkBehaviour
{
    public AudioClip[] BallHitBall;
    public AudioClip[] BallHitWall;
    public AudioClip[] BallFall;
    private AudioClip BallClip;
    public Rigidbody BallRigidbody;
    private GameManager gameManager;
    private bool isOnTable = true;
    public AudioSource audioSource;
    public int ballNumber = 0;
    public string ballType;
    public NetworkVariable<Vector3> NetworkPosition = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<Quaternion> NetworkRotation = new NetworkVariable<Quaternion>(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector3> NetworkVelocity = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector3> NetworkAngularVelocity = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Server);
    private readonly float correctionFactor = 0.1f;
    private readonly float positionThreshold = 0.01f;
    private readonly float velocityThreshold = 0.1f;
    private readonly float angularVelocityThreshold = 0.1f;
    private readonly float angleThreshold = 30f;

    int count = 0;
    void FixedUpdate()
    {
        count = (count + 1) % 1001;
        if (IsServer)
        {
            NetworkPosition.Value = transform.position;
            NetworkVelocity.Value = BallRigidbody.velocity;
            NetworkAngularVelocity.Value = BallRigidbody.angularVelocity;
            if (count % 5 == 0)
            {
                NetworkRotation.Value = transform.rotation;
            }
        }
        else
        {
            if (isOnTable)
            {
                Vector3 positionError = NetworkPosition.Value - transform.position;
                Vector3 velocityError = NetworkVelocity.Value - BallRigidbody.velocity;
                Vector3 angularVelocityError = NetworkAngularVelocity.Value - BallRigidbody.angularVelocity;
                if (count % 5 == 0)
                {
                    transform.rotation = NetworkRotation.Value;
                }
                float angleDifference = Vector3.Angle(BallRigidbody.velocity, NetworkVelocity.Value);
                if (angleDifference > angleThreshold)
                {
                    // 방향 변화가 큰 경우 즉시 서버의 상태로 동기화
                    transform.position = NetworkPosition.Value;
                    BallRigidbody.velocity = NetworkVelocity.Value;
                    BallRigidbody.angularVelocity = NetworkAngularVelocity.Value;
                }
                else
                {
                    if (positionError.magnitude > positionThreshold) transform.position = Vector3.Lerp(transform.position, NetworkPosition.Value, correctionFactor);
                    if (velocityError.magnitude > velocityThreshold) BallRigidbody.velocity = Vector3.Lerp(BallRigidbody.velocity, NetworkVelocity.Value, correctionFactor);
                    if (angularVelocityError.magnitude > angularVelocityThreshold) 
                        BallRigidbody.angularVelocity = Vector3.Lerp(BallRigidbody.angularVelocity, NetworkAngularVelocity.Value, correctionFactor);
                }
                
            }
            else
            {
                if(count == 1000)
                {
                    transform.position = NetworkPosition.Value;
                    BallRigidbody.velocity = NetworkVelocity.Value;
                }
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        float volume = Mathf.Clamp(BallRigidbody.velocity.magnitude / 5.0f, 0.1f, 1.0f);

        if (collision.gameObject.CompareTag("SolidBall") || collision.gameObject.CompareTag("StripedBall") || collision.gameObject.CompareTag("CueBall"))
        {
            int index = UnityEngine.Random.Range(0, BallHitBall.Length);
            PlayBallHitAudioClientRpc(index, volume);
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            PlayBallHitWallAudioClientRpc(0, volume);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;
        if (other.CompareTag("Hole") && isOnTable)
        {
            isOnTable = false;
            Debug.Log($"{name}이 구멍에 빠짐");
            StartCoroutine(ByeBye());
            gameManager.BallFell(gameObject);
        }
        if (other.CompareTag("Floor") && isOnTable)
        {
            Debug.Log($"{name}이 바닥에 떨어짐");
            if (!CompareTag("CueBall"))
            {
                StartCoroutine(BallFellToFloor());
            }
            else
            {
                gameManager.SetFreeBallServerRpc(true);
                gameManager.SethasExtraTurnServerRpc(false);
            }
        }
    }
    [ClientRpc]
    void PlayBallHitAudioClientRpc(int clipIndex, float volume)
    {
        if (BallHitBall != null && clipIndex < BallHitBall.Length)
        {
            audioSource.PlayOneShot(BallHitBall[clipIndex], volume);
        }
    }

    [ClientRpc]
    void PlayBallHitWallAudioClientRpc(int clipIndex, float volume)
    {
        if (BallHitWall != null && clipIndex < BallHitWall.Length)
        {
            audioSource.PlayOneShot(BallHitWall[clipIndex], volume);
        }
    }
    [ClientRpc]
    void PlayBallFallAudioClientRpc(int clipIndex)
    {
        if (BallFall != null && clipIndex < BallFall.Length)
        {
            audioSource.PlayOneShot(BallFall[clipIndex], 1.0f);
        }
    }
    public bool IsBallStopped()
    {
        if (transform.position.y <= -1f) return true;
        if (BallRigidbody.velocity.magnitude < 0.1f && BallRigidbody.angularVelocity.magnitude < 0.8f) BallRigidbody.angularVelocity = Vector3.zero;
        return BallRigidbody.velocity.magnitude < 0.1f && BallRigidbody.angularVelocity.magnitude < 0.1f;
    }

    public void StopMove()
    {
        if (transform.position.y <= -1f) return;
        BallRigidbody.constraints = RigidbodyConstraints.FreezePosition;
    }

    private IEnumerator ByeBye()
    {
        int index = Random.Range(0, BallFall.Length);
        BallClip = BallFall[index];
        PlayBallFallAudioClientRpc(index);
        yield return new WaitForSeconds(0.3f);
        transform.position = new(5.475f, -1, 0.805f);
        if(CompareTag("CueBall")) isOnTable = true;
    }

    private IEnumerator BallFellToFloor()
    {
        
        while (!gameManager.AreAllBallsStopped())
        {
            yield return null; // 다음 프레임까지 대기
        }

        ResetBallPositionServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    private void ResetBallPositionServerRpc()
    {
        Vector3 respawnPosition = new Vector3(5.475f, 0.33f, 0f);
        float ballRadius = 0.32f;
        float checkRadius = ballRadius * 1.1f;

        bool positionFound = false;
        int maxAttempts = 100;
        for (int i = 0; i < maxAttempts; i++)
        {
            // 일정 범위 내에서 무작위 오프셋을 적용하여 위치 시도
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
            Vector3 testPosition = respawnPosition + randomOffset;

            // 해당 위치에 다른 공들이 있는지 확인
            Collider[] colliders = Physics.OverlapSphere(testPosition, checkRadius);

            bool isOverlapping = false;
            foreach (Collider collider in colliders)
            {
                if (collider.gameObject != gameObject && (collider.CompareTag("SolidBall") || collider.CompareTag("StripedBall") || collider.CompareTag("CueBall")))
                {
                    isOverlapping = true;
                    break;
                }
            }

            if (!isOverlapping)
            {
                // 겹치지 않는 위치를 찾음
                respawnPosition = testPosition;
                positionFound = true;
                break;
            }
        }

        if (!positionFound)
        {
            Debug.LogWarning("다른 공들과 겹치지 않는 위치를 찾을 수 없습니다.");
        }

        transform.position = respawnPosition;
        transform.rotation = Quaternion.Euler(90f, 0, 0);
        BallRigidbody.velocity = Vector3.zero;
        BallRigidbody.angularVelocity = Vector3.zero;

        NetworkPosition.Value = transform.position;
        NetworkVelocity.Value = BallRigidbody.velocity;
        NetworkAngularVelocity.Value = Vector3.zero;

        ForceSetBallClientRpc();
    }

    [ClientRpc]
    private void ForceSetBallClientRpc()
    {
        transform.position = NetworkPosition.Value;
        BallRigidbody.velocity = NetworkVelocity.Value;
        transform.rotation = NetworkRotation.Value;
    }

    public override void OnNetworkSpawn()
    {

        audioSource = GetComponent<AudioSource>();
        gameManager = FindObjectOfType<GameManager>();


        if (CompareTag("CueBall"))
        {
            ballType = "CueBall";
            ballNumber = 0;
        }
        else if (CompareTag("EightBall"))
        {
            ballType = "EightBall";
            ballNumber = 8;
        }
        else if (CompareTag("SolidBall"))
        {
            ballType = "SolidBall";
        }
        else if (CompareTag("StripedBall"))
        {
            ballType = "StripedBall";
        }
        BallRigidbody = GetComponent<Rigidbody>();

    }
}
