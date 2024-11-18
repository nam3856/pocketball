using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Cysharp.Threading.Tasks;
using static CueBallController;
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
    private readonly float minPower = 0.1f;
    private readonly float maxPower = 20f;
    private float angleAdjustmentSpeed = 20f; // 각도 조절 속도 (필요에 따라 조정)

    private GameManager gameManager;


    public CameraController cameraController;
    private Vector2 hitPoint = Vector2.zero;
    public Transform hitPointIndicator;
    public LayerMask cueBallLayerMask;
    public event Action OnHitBall;
    private float cueOffset = -0.1f; // 큐볼에서 큐대까지의 거리

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
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
        int count = 0;

        await UniTask.Delay(100);
        if(!GameManager.Instance.freeBall.Value) ShowCue();
        while (GameManager.Instance.freeBall.Value)
        {
            await UniTask.Yield();
        }
        ShowCue();
        while (GameManager.Instance.GetMyPlayerNumber() != GameManager.Instance.playerTurn.Value || !IsOwner)
        {
            //Debug.Log($"not your turn{GameManager.Instance.GetMyPlayerNumber()} {GameManager.Instance.playerTurn.Value}");
            count++;

            await UniTask.Delay(100);
            if (count >= 10) return;
        }
        await UniTask.Delay(100);
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
        HideCue();
    }

    public void StopCueControl()
    {
        isCueControlActive = false;
    }

    private bool isCueControlActive = false;

    public void FollowMousePointer()
    {
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
        UpdateCuePositionServerRpc(Cue.transform.position, Cue.transform.rotation);
        // 마우스 왼쪽 버튼 클릭으로 방향을 고정
        if (Input.GetMouseButtonDown(0))
        {
            isDirectionFixed = true;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void UpdateCuePositionServerRpc(Vector3 position, Quaternion rotation)
    {
        UpdateCuePositionClientRpc(position, rotation);
    }

    // 다른 클라이언트에서도 위치와 회전을 업데이트하는 ClientRpc
    [ClientRpc]
    private void UpdateCuePositionClientRpc(Vector3 position, Quaternion rotation)
    {
        Cue.transform.position = position;
        Cue.transform.rotation = rotation;
    }
    void SetDirectionAndPower()
    {
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
            UpdateCuePositionServerRpc(Cue.transform.position, Cue.transform.rotation);
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
            UpdateCuePositionServerRpc(Cue.transform.position, Cue.transform.rotation);
        }

        // 스페이스바를 누르면 공을 침
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameManager.Instance.HitConfirmedServerRpc();
            isCueControlActive = false;
            StartCoroutine(HitBall());
        }

        // 마우스 왼쪽 버튼 클릭으로 방향 고정 해제
        if (Input.GetMouseButtonDown(0))
        {
            isDirectionFixed = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestShootServerRpc(Vector3 direction, float power, Vector2 hitPoint)
    {
        // 입력 검증...
        if (power > maxPower) power = maxPower;
        if (power < minPower) power = minPower;

        Debug.Log($"Hit {direction} {power} {hitPoint}");
        // 큐볼에 힘 적용
        cueBallController.HitBall(direction, power, hitPoint);

        // 다른 클라이언트에게 샷 정보 전달
        ApplyShootClientRpc(direction, power, hitPoint);
        gameManager.CheckBallsMovementAsync().Forget();

    }

    [ClientRpc]
    void ApplyShootClientRpc(Vector3 direction, float power, Vector2 hitPoint)
    {
        if (IsOwner||IsHost)
            return;
        // 로컬에서 샷 적용
        cueBallController.HitBall(direction, power, hitPoint);
    }

    
    IEnumerator HitBall()
    {
        if (isHitting) yield break;
        isHitting = true;

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
        power = 0.5f;
        // 큐대를 초기 위치로 복귀
        cueOffset = power;
        Cue.transform.position = CueBall.position + CueDirection * cueOffset;
        hitPoint = Vector2.zero;
        OnHitBall?.Invoke();
        HideCue();
    }

    [ServerRpc(RequireOwnership = false)]
    internal void SetHitPointServerRPC(Vector2 normalizedPoint)
    {
        SetHitPointClientRPC(normalizedPoint);
    }
    [ClientRpc]
    internal void SetHitPointClientRPC(Vector2 normalizedPoint)
    {
        hitPoint = normalizedPoint;
    }

    public void ShowCue()
    {
        Cue.GetComponent<MeshRenderer>().enabled = true;
        ShowCueClientRpc();
    }

    public void HideCue()
    {
        Cue.GetComponent<MeshRenderer>().enabled = false;
        HideCueClientRpc();
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

    [ClientRpc]
    private void ShowCueClientRpc()
    {
        Cue.GetComponent<MeshRenderer>().enabled = true;
    }

    [ClientRpc]
    private void HideCueClientRpc()
    {
        Cue.GetComponent<MeshRenderer>().enabled = false;
    }
}

