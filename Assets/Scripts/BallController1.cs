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
    private void Start()
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
            Debug.Log($"{name}ÀÌ ±¸¸Û¿¡ ºüÁü");
            StartCoroutine(ByeBye());
            gameManager.BallFell(gameObject);
        }
        if (other.CompareTag("Floor") && isOnTable)
        {
            Debug.Log($"{name}ÀÌ ¹Ù´Ú¿¡ ¶³¾îÁü");
            if (!CompareTag("CueBall"))
            {
                transform.position = new(0f, 0.5f, 0f);
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


}
