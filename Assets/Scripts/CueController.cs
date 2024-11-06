using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CueController : MonoBehaviour
{
    private Vector3 mousePos;
    private float angle = 0.0f;
    private bool isDirectionFixed = false;
    public bool isHitting = false;
    public CueBallController cueBallController;

    public Vector3 CueDirection;
    public GameObject Cue;
    public Transform CueBall;
    public Transform cueTransform;

    private float power = 1f;
    private float minPower = 0.1f;
    private float maxPower = 5f;
    private float angleAdjustmentSpeed = 0.1f;

    private GameManager gameManager;

    private float tableMinX = -7.2f;
    private float tableMaxX = 7.2f;
    private float tableMinZ = -3.3f;
    private float tableMaxZ = 3.3f;

    public CameraController cameraController;
    private Vector2 hitPoint = Vector2.zero;
    public Transform hitPointIndicator;
    public LayerMask cueBallLayerMask;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        cameraController = FindObjectOfType<CameraController>();
    }

    void Update()
    {
        if (cameraController.isTransitioning || cameraController.isAiming)
        {
            return;
        }
        // 프리볼 상태 처리
        if (gameManager.freeBall && !gameManager.ballsAreMoving)
        {
            HandleFreeBallPlacement();
            return;
        }

        // 모든 공이 멈췄는지 확인
        if (gameManager.ballsAreMoving)
        {
            Cue.GetComponent<SpriteRenderer>().enabled = false;
            isDirectionFixed = false;
            return;
        }
        if (isHitting) return;
        // 공이 멈췄으면 큐를 보이게 함
        Cue.GetComponent<SpriteRenderer>().enabled = true;

        // 마우스 위치를 가져옴
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 큐 방향이 고정되지 않았다면 마우스 위치로 큐 방향 설정
        if (!isDirectionFixed)
        {
            Vector3 targetDir = (mousePos - CueBall.position).normalized;
            angle = Mathf.Atan2(targetDir.x, targetDir.z) * Mathf.Rad2Deg;
            Cue.transform.eulerAngles = new Vector3(90, angle, 0);

            // 큐를 공에 붙임
            Cue.transform.position = CueBall.position + 0.01f * targetDir;
        }
        else // 큐 방향이 고정되었을 때
        {
            // 좌우 방향키로 각도 조절
            float horizontalInput = Input.GetAxis("Horizontal");
            if (horizontalInput != 0)
            {
                angle += horizontalInput * angleAdjustmentSpeed;
                Cue.transform.eulerAngles = new Vector3(90, angle, 0);

                // 큐 위치 업데이트
                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Cue.transform.position = CueBall.position + 0.01f * direction;
            }

            // 상하 방향키로 파워 조절
            float verticalInput = Input.GetAxis("Vertical");
            if (verticalInput != 0)
            {
                power += verticalInput * Time.deltaTime;
                power = Mathf.Clamp(power, minPower, maxPower);

                // 큐와 공 사이의 거리 조절
                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Cue.transform.position = CueBall.position + direction * power;
            }
            if (Input.GetMouseButtonDown(1))
            {
                isDirectionFixed = true;
                cameraController.StartAimMode();
                StartCoroutine(SelectHitPoint());
            }
            // 스페이스바를 누르면 공을 침
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartCoroutine(HitBall());
            }

            if (!cameraController.isTransitioning && cameraController.isAiming)
            {
                UpdateHitPointIndicator();
            }
        }

        // 마우스를 클릭하면 큐 방향 고정
        if (Input.GetMouseButtonDown(0) && !isDirectionFixed)
        {
            isDirectionFixed = true;
        }

        
    }

    void UpdateHitPointIndicator()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.Log("Selecting Hit Point");
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == CueBall.gameObject)
            {
                // 타격점 표시기 위치 업데이트
                Vector3 hitPointOnCueBall = hit.point;
                hitPointIndicator.position = hitPointOnCueBall + hit.normal * 0.01f; // 약간 띄워서 표시

                // 타격점 계산 (-1, -1) ~ (1, 1)
                Vector3 localHitPoint = CueBall.InverseTransformPoint(hit.point);
                float radius = CueBall.GetComponent<SphereCollider>().radius;
                hitPoint = new Vector2(localHitPoint.x / radius, localHitPoint.y / radius);

                // 타격점 표시기가 카메라를 향하도록 회전
                hitPointIndicator.forward = Camera.main.transform.forward;
            }
        }

    }
    IEnumerator SelectHitPoint()
    {
        hitPointIndicator.gameObject.SetActive(true);
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }
        hitPointIndicator.gameObject.SetActive(false);
        // 타격점 선택 완료
        cameraController.EndAimMode();
    }

    void HandleFreeBallPlacement()
    {
        // 큐를 숨김
        Cue.GetComponent<SpriteRenderer>().enabled = false;

        // 마우스 위치를 가져옴
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.y = CueBall.position.y;

        // 마우스 위치를 테이블 영역 내로 제한
        float xPos = Mathf.Clamp(mousePos.x, tableMinX, tableMaxX);
        float zPos = Mathf.Clamp(mousePos.z, tableMinZ, tableMaxZ);
        Vector3 newPosition = new Vector3(xPos, 0.3f, zPos);

        
        CueBall.GetComponent<Rigidbody>().velocity = Vector3.zero;
        CueBall.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        // 해당 위치에 다른 공이 있는지 검사
        if (!IsPositionValid(newPosition))
        {
            return;
        }

        // 큐볼을 해당 위치로 이동
        CueBall.position = newPosition;
        
        // 마우스 왼쪽 버튼 클릭 시 위치 확정
        if (Input.GetMouseButtonDown(0))
        {
            gameManager.freeBall = false;
        }
    }

    bool IsPositionValid(Vector3 position)
    {
        float radius = 0.32f; // 공의 반지름 (필요에 따라 조정)
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject != CueBall.gameObject && collider.gameObject.CompareTag("SolidBall") || collider.gameObject.CompareTag("StripedBall") || collider.gameObject.CompareTag("EightBall"))
            {
                return false;
            }
        }
        return true;
    }


    IEnumerator HitBall()
    {
        CueDirection = (Cue.transform.position - CueBall.position).normalized;

        // 큐를 뒤로 당겼다가 앞으로 미는 애니메이션
        Vector3 startPosition = Cue.transform.position;
        Vector3 endPosition = CueBall.position + 0.01f * CueDirection;
        float animTime = 0.2f;
        float startTime = Time.time;

        while (Time.time - startTime < animTime)
        {
            Cue.transform.position = Vector3.Lerp(startPosition, endPosition, (Time.time - startTime) / animTime);
            yield return null;
        }

        Cue.transform.position = endPosition;

        Cue.GetComponent<SpriteRenderer>().enabled = false;

        cueBallController.HitBall(CueDirection, power, hitPoint);

        isHitting = true;
        // 초기화
        isDirectionFixed = false;
        power = 1f; // 파워 초기화
    }
}
