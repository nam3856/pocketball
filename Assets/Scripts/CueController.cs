using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Cysharp.Threading.Tasks;
public class CueController : NetworkBehaviour
{
    private Vector3 mousePos;
    private float angle = 0.0f;
    private bool isDirectionFixed = false;
    public bool isHitting = false;
    public CueBallController cueBallController;

    public Vector3 CueDirection;
    public GameObject Cue;
    public Transform CueBall;

    private float power = 1f;
    private float minPower = 0.1f;
    private float maxPower = 20f;
    private float angleAdjustmentSpeed = 20f; // 각도 조절 속도 (필요에 따라 조정)

    private GameManager gameManager;


    public CameraController cameraController;
    private Vector2 hitPoint = Vector2.zero;
    public Transform hitPointIndicator;
    public LayerMask cueBallLayerMask;
    public event Action OnHitBall;

    private float cueOffset = -0.1f; // 큐볼에서 큐대까지의 거리

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        cameraController = FindObjectOfType<CameraController>();

        HitPointIndicatorController hitPointIndicator = FindObjectOfType<HitPointIndicatorController>();
        if (hitPointIndicator != null)
        {
            hitPointIndicator.SubscribeEvent(this);
        }
    }


    public async UniTaskVoid StartCueControlAsync()
    {
        isCueControlActive = true;
        if (GameManager.Instance.GetMyPlayerNumber() != GameManager.Instance.playerTurn.Value)
        {
            Debug.Log($"not your turn{GameManager.Instance.GetMyPlayerNumber()} {GameManager.Instance.playerTurn.Value}");
            return;
        }
        while (GameManager.Instance.freeBall.Value)
        {
            await UniTask.Yield();
        }
        while (isCueControlActive)
        {
            if (!isDirectionFixed)
            {
                FollowMousePointer();
            }
            else
            {
                SetDirectionAndPower();
            }
            await UniTask.Yield();
        }
    }

    public void StopCueControl()
    {
        isCueControlActive = false;
    }

    private bool isCueControlActive = false;

    public void FollowMousePointer()
    {
        if (!IsOwner) return;
        // 마우스 위치를 가져옴
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.y = 0.33f;

        Vector3 targetDir = (mousePos - CueBall.position).normalized;
        angle = Mathf.Atan2(targetDir.x, targetDir.z) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        CueDirection = rotation * Vector3.forward;

        // 큐의 회전 및 위치 설정

        Cue.transform.rotation = Quaternion.LookRotation(CueDirection);
        Cue.transform.position = CueBall.position + CueDirection * cueOffset;
        Debug.Log($"{mousePos} {Cue.transform.rotation} {Cue.transform.position}");
        // 마우스 왼쪽 버튼 클릭으로 방향을 고정
        if (Input.GetMouseButtonDown(0))
        {
            isDirectionFixed = true;
        }
    }

    void SetDirectionAndPower()
    {
        if (!IsOwner) return;
        // 좌우 방향키로 각도 조절
        float horizontalInput = Input.GetAxis("Horizontal");
        if (horizontalInput != 0)
        {
            angle += horizontalInput * angleAdjustmentSpeed * Time.deltaTime;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            CueDirection = rotation * Vector3.forward;

            // 큐의 회전 및 위치 업데이트
            Cue.transform.rotation = Quaternion.LookRotation(CueDirection);
            Cue.transform.position = CueBall.position + CueDirection * cueOffset;

            Debug.Log($"{mousePos} {Cue.transform.rotation} {Cue.transform.position}");
        }

        // 상하 방향키로 파워 조절
        float verticalInput = Input.GetAxis("Vertical");
        if (verticalInput != 0)
        {
            power += verticalInput * Time.deltaTime * 8;
            power = Mathf.Clamp(power, minPower, maxPower);

            // 큐와 공 사이의 거리 조절
            cueOffset = Mathf.Clamp(power / 8, minPower, maxPower / 8);
            Cue.transform.position = CueBall.position + CueDirection * cueOffset;

            Debug.Log($"{mousePos} {Cue.transform.rotation} {Cue.transform.position}");
        }

        // 스페이스바를 누르면 공을 침
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameManager.Instance.HitConfirmedServerRpc();
            StartCoroutine(HitBall());
        }

        // 마우스 왼쪽 버튼 클릭으로 방향 고정 해제
        if (Input.GetMouseButtonDown(0))
        {
            isDirectionFixed = false;
        }
    }

    [ServerRpc]
    void RequestShootServerRpc(Vector3 direction, float power, Vector2 hitPoint)
    {
        if (!IsOwner)
            return;
        // 입력 검증...
        Debug.Log("Hit");
        // 큐볼에 힘 적용
        cueBallController.HitBall(direction, power, hitPoint);

        // 다른 클라이언트에게 샷 정보 전달
        ApplyShootClientRpc(direction, power, hitPoint);
        gameManager.CheckBallsMovementAsync().Forget();

    }

    [ClientRpc]
    void ApplyShootClientRpc(Vector3 direction, float power, Vector2 hitPoint)
    {
        if (IsOwner)
            return;

        // 로컬에서 샷 적용
        cueBallController.HitBall(direction, power, hitPoint);
    }

    
    IEnumerator HitBall()
    {
        isHitting = true;

        // 큐대를 뒤로 당겼다가 앞으로 미는 애니메이션
        float animTime = 0.2f;
        float startTime = Time.time;

        Vector3 startPosition = Cue.transform.position;
        float cueOffsetAfterHit = 0.5f; // 타격 후 큐대 위치
        Vector3 endPosition = CueBall.position + CueDirection * cueOffsetAfterHit;

        while (Time.time - startTime < animTime)
        {
            float t = (Time.time - startTime) / animTime;
            Cue.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        Cue.transform.position = endPosition;
        // 큐볼에 힘 전달
        RequestShootServerRpc(CueDirection, power, hitPoint);


        // 초기화
        isDirectionFixed = false;
        isHitting = false;
        power = 1f;
        // 큐대를 초기 위치로 복귀
        cueOffset = -power;
        Cue.transform.position = CueBall.position + CueDirection * cueOffset;
        hitPoint = Vector2.zero;
        OnHitBall?.Invoke();
        
    }

    internal void SetHitPoint(Vector2 normalizedPoint)
    {
        hitPoint = normalizedPoint;
    }

    public void ShowCue()
    {
        Cue.GetComponent<MeshRenderer>().enabled = true;
    }

    public void HideCue()
    {
        Cue.GetComponent<MeshRenderer>().enabled = false;
    }

    public void SetOwnerClientId(ulong clientId)
    {
        GetComponent<NetworkObject>().ChangeOwnership(clientId);
    }

    public void EnableCue()
    {
        Cue.SetActive(true);
    }

    public void DisableCue()
    {
        Cue.SetActive(false);
    }
}

