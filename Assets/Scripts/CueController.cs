using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
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

    private float tableMinX = -7.5f;
    private float tableMaxX = 7.5f;
    private float tableMinZ = -3.3f;
    private float tableMaxZ = 3.3f;

    public CameraController cameraController;
    private Vector2 hitPoint = Vector2.zero;
    public Transform hitPointIndicator;
    public LayerMask cueBallLayerMask;
    public GameObject Box;
    public event Action OnHitBall;
    private NetworkObject parentNetworkObject;

    private float cueOffset = -0.1f; // 큐볼에서 큐대까지의 거리

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        cameraController = FindObjectOfType<CameraController>();

        parentNetworkObject = GetComponentInParent<NetworkObject>();

        if (parentNetworkObject == null)
        {
            Debug.LogError("Parent NetworkObject not found!");
            return;
        }
    }

    void Update()
    {
        if (!parentNetworkObject.IsOwner)
        {
            Debug.Log("Not Owner, return");
            return;
        }

        int myPlayerNumber = GameManager.Instance.GetMyPlayerNumber();
        if (GameManager.Instance.playerTurn.Value != myPlayerNumber)
        {
            Debug.Log($"Current Player Turn = {GameManager.Instance.playerTurn.Value}, You = {myPlayerNumber}");
            return;
        }
        // 프리볼 상태 처리
        if (gameManager.freeBall.Value && !gameManager.ballsAreMoving.Value)
        {
            HandleFreeBallPlacement();
            return;
        }

        // 모든 공이 멈췄는지 확인
        if (gameManager.ballsAreMoving.Value)
        {
            isDirectionFixed = false;
            return;
        }
        if (isHitting) return;

        // 마우스 위치를 가져옴
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.y = CueBall.position.y;

        // 큐 방향이 고정되지 않았다면 마우스 위치로 큐 방향 설정
        if (!isDirectionFixed)
        {
            Vector3 targetDir = (mousePos - CueBall.position).normalized;
            angle = Mathf.Atan2(targetDir.x, targetDir.z) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            CueDirection = rotation * Vector3.forward;

            // 큐의 회전 및 위치 설정
            Cue.transform.rotation = Quaternion.LookRotation(CueDirection);
            Cue.transform.position = CueBall.position + CueDirection * cueOffset;
        }
        else // 큐 방향이 고정되었을 때
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
            }

            // 상하 방향키로 파워 조절
            float verticalInput = Input.GetAxis("Vertical");
            if (verticalInput != 0)
            {
                power += verticalInput * Time.deltaTime*8;
                power = Mathf.Clamp(power, minPower, maxPower);

                // 큐와 공 사이의 거리 조절
                cueOffset = Mathf.Clamp(-power/8, -maxPower/8, -minPower);
                Cue.transform.position = CueBall.position + CueDirection * cueOffset;
            }

            // 스페이스바를 누르면 공을 침
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartCoroutine(HitBall());
                
            }
        }

        // 마우스를 클릭하면 큐 방향 고정
        if (Input.GetMouseButtonDown(0) && !isDirectionFixed)
        {
            isDirectionFixed = true;
        }
    }
    [ServerRpc]
    void RequestShootServerRpc(Vector3 direction, float power, Vector2 hitPoint)
    {
        // 입력 검증...

        // 큐볼에 힘 적용
        cueBallController.HitBall(direction, power, hitPoint);

        // 다른 클라이언트에게 샷 정보 전달
        ApplyShootClientRpc(direction, power, hitPoint);
        gameManager.CheckBallsMovementAsync().Forget();

    }

    [ClientRpc]
    void ApplyShootClientRpc(Vector3 direction, float power, Vector2 hitPoint)
    {
        if (parentNetworkObject.IsOwner)
            return;

        // 로컬에서 샷 적용
        cueBallController.HitBall(direction, power, hitPoint);
    }

    void HandleFreeBallPlacement()
    {
        // 마우스 위치를 가져옴
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.y = CueBall.position.y;

        // 마우스 위치를 테이블 영역 내로 제한
        float xPos = Mathf.Clamp(mousePos.x, tableMinX, tableMaxX);
        float zPos = Mathf.Clamp(mousePos.z, tableMinZ, tableMaxZ);
        Vector3 newPosition = new Vector3(xPos, 0.3f, zPos);

        // 해당 위치에 다른 공이 있는지 검사 (클라이언트에서 임시로 검사)
        if (!IsPositionValid(newPosition))
        {
            return;
        }

        // 마우스 왼쪽 버튼 클릭 시 위치 확정
        if (Input.GetMouseButtonDown(0))
        {
            // 서버에 큐볼 위치 변경 요청
            RequestFreeBallPlacementServerRpc(newPosition);
            // freeBall 상태 변경을 서버에 요청
            gameManager.SetFreeBallServerRpc(false);
        }
    }


    [ServerRpc]
    void RequestFreeBallPlacementServerRpc(Vector3 newPosition)
    {
        // 해당 위치에 다른 공이 있는지 서버에서 검사
        if (!IsPositionValidOnServer(newPosition))
            return;

        // 큐볼의 위치를 서버에서 변경
        CueBall.position = newPosition;
    }

    bool IsPositionValidOnServer(Vector3 position)
    {
        float radius = 0.32f;
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject != CueBall.gameObject && (collider.gameObject.CompareTag("SolidBall") || collider.gameObject.CompareTag("StripedBall") || collider.gameObject.CompareTag("EightBall")))
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
            if (collider.gameObject != CueBall.gameObject && (collider.gameObject.CompareTag("SolidBall") || collider.gameObject.CompareTag("StripedBall") || collider.gameObject.CompareTag("EightBall")))
            {
                return false;
            }
        }
        return true;
    }

    IEnumerator HitBall()
    {
        isHitting = true;

        // 큐대를 뒤로 당겼다가 앞으로 미는 애니메이션
        float animTime = 0.2f;
        float startTime = Time.time;

        Vector3 startPosition = Cue.transform.position;
        float cueOffsetAfterHit = -0.5f; // 타격 후 큐대 위치
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
        Cue.GetComponentInChildren<MeshRenderer>().enabled = true;
    }

    public void HideCue()
    {
        Cue.GetComponentInChildren<MeshRenderer>().enabled = false;
    }
}

