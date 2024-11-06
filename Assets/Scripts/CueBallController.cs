using UnityEngine;

public class CueBallController : MonoBehaviour
{
    public AudioSource audioSource;
    public Rigidbody CueBallRigidbody;
    public CueController cueController;
    private float respawnHeight = -2f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void HitBall(Vector3 direction, float power, Vector2 hitPoint)
    {
        // 타격점 위치 계산
        Vector3 hitPoint3D = new Vector3(hitPoint.x, 0, hitPoint.y) * 0.1f;
        Vector3 forcePosition = transform.position + hitPoint3D;

        // 힘 적용
        CueBallRigidbody.AddForceAtPosition(direction * 500f * power, forcePosition, ForceMode.Impulse);
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
