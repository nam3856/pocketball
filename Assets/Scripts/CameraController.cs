using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Camera mainCamera;
    public Transform cueStick; // 큣대의 Transform
    public Transform cueBall; // 큐볼의 Transform

    public Vector3 originalPosition;
    public Quaternion originalRotation;
    public float originalSize;

    public Vector3 aimPosition;
    public Quaternion aimRotation;
    public float aimSize = 2f;

    public float transitionSpeed = 4f;

    public bool isTransitioning = false;
    public bool isAiming = false;


    void Start()
    {
        // 카메라의 원래 위치와 회전 저장
        originalPosition = mainCamera.transform.position;
        originalRotation = mainCamera.transform.rotation;
        originalSize = mainCamera.orthographicSize;
    }

    public void StartAimMode()
    {
        // 카메라의 목표 위치를 큣대의 뒤쪽으로 설정
        aimPosition = cueStick.position - cueStick.forward * 0.5f + Vector3.up * 0.1f;

        aimPosition.x -= 6f;

        // 큣대의 회전을 가져옴
        aimRotation = cueStick.rotation;

        // x축 회전을 원하는 각도로 조정
        Vector3 eulerAngles = aimRotation.eulerAngles;
        eulerAngles.x = 10f;
        eulerAngles.y += 180f;
        aimRotation = Quaternion.Euler(eulerAngles);

        isTransitioning = true;
        isAiming = true;
    }

    public void EndAimMode()
    {
        isTransitioning = true;
        isAiming = false;
    }

    void Update()
    {
        if (isTransitioning)
        {
            if (isAiming)
            {
                // 카메라를 목표 위치로 이동
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, aimPosition, Time.deltaTime * transitionSpeed);
                mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, aimRotation, Time.deltaTime * transitionSpeed);
                mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, aimSize, Time.deltaTime * transitionSpeed);

                if (Vector3.Distance(mainCamera.transform.position, aimPosition) < 0.01f)
                {
                    isTransitioning = false;
                }
            }
            else
            {
                // 카메라를 원래 위치로 이동
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, originalPosition, Time.deltaTime * transitionSpeed * 2);
                mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, originalRotation, Time.deltaTime * transitionSpeed * 2);
                mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, originalSize, Time.deltaTime * transitionSpeed * 2);

                if (Vector3.Distance(mainCamera.transform.position, originalPosition) < 0.01f)
                {
                    isTransitioning = false;
                }
            }
        }
    }
}
