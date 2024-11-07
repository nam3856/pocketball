using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Camera mainCamera;
    public Transform cueStick; // �p���� Transform
    public Transform cueBall; // ť���� Transform

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
        // ī�޶��� ���� ��ġ�� ȸ�� ����
        originalPosition = mainCamera.transform.position;
        originalRotation = mainCamera.transform.rotation;
        originalSize = mainCamera.orthographicSize;
    }

    public void StartAimMode()
    {
        // ī�޶��� ��ǥ ��ġ�� �p���� �������� ����
        aimPosition = cueStick.position - cueStick.forward * 0.5f + Vector3.up * 0.1f;

        aimPosition.x -= 6f;

        // �p���� ȸ���� ������
        aimRotation = cueStick.rotation;

        // x�� ȸ���� ���ϴ� ������ ����
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
                // ī�޶� ��ǥ ��ġ�� �̵�
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
                // ī�޶� ���� ��ġ�� �̵�
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
