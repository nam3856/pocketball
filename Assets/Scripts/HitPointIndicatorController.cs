using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HitPointIndicatorController : MonoBehaviour, IPointerClickHandler
{
    public Image cueBallImage; // ť�� �̹��� (Inspector���� �Ҵ�)
    public CueController cueController; // CueController ��ũ��Ʈ ����
    public Image HitPointIndicator;
    void Start()
    {
        if (cueBallImage == null)
            cueBallImage = GetComponent<Image>();
        cueController.OnHitBall += ResetIndicatorPosition;
    }

    private void ResetIndicatorPosition()
    {
        Debug.Log("�̺�Ʈ ����");
        HitPointIndicator.rectTransform.anchoredPosition = Vector2.zero;
    }

    void OnDestroy()
    {
        // �̺�Ʈ ���� ����
        cueController.OnHitBall -= ResetIndicatorPosition;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        RectTransform rectTransform = cueBallImage.rectTransform;

        // Ŭ���� ��ġ�� �̹����� ���� ��ǥ�� ��ȯ
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint);

        HitPointIndicator.rectTransform.anchoredPosition = localPoint;
        // ���� ��ǥ�� -1���� 1 ���̷� ����ȭ
        Vector2 normalizedPoint = new Vector2(
            (localPoint.x / (rectTransform.rect.width * 0.5f)),
            (localPoint.y / (rectTransform.rect.height * 0.5f))
        );

        // ���� ���� (-1, -1) ~ (1, 1)
        normalizedPoint = Vector2.ClampMagnitude(normalizedPoint, 1f);

        // y�� ���� (�ʿ��� ���)
        //normalizedPoint.y = -normalizedPoint.y;

        Debug.Log("Normalized Hit Point: " + normalizedPoint);

        // CueController�� Ÿ���� ����
        cueController.SetHitPoint(normalizedPoint);
    }
}
