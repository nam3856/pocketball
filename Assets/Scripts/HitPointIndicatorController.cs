using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HitPointIndicatorController : MonoBehaviour, IPointerClickHandler
{
    public Image cueBallImage; // 큐볼 이미지 (Inspector에서 할당)
    public CueController cueController; // CueController 스크립트 참조
    public Image HitPointIndicator;
    void Start()
    {
        if (cueBallImage == null)
            cueBallImage = GetComponent<Image>();
        cueController.OnHitBall += ResetIndicatorPosition;
    }

    private void ResetIndicatorPosition()
    {
        Debug.Log("이벤트 받음");
        HitPointIndicator.rectTransform.anchoredPosition = Vector2.zero;
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        cueController.OnHitBall -= ResetIndicatorPosition;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        RectTransform rectTransform = cueBallImage.rectTransform;

        // 클릭한 위치를 이미지의 로컬 좌표로 변환
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint);

        HitPointIndicator.rectTransform.anchoredPosition = localPoint;
        // 로컬 좌표를 -1에서 1 사이로 정규화
        Vector2 normalizedPoint = new Vector2(
            (localPoint.x / (rectTransform.rect.width * 0.5f)),
            (localPoint.y / (rectTransform.rect.height * 0.5f))
        );

        // 범위 제한 (-1, -1) ~ (1, 1)
        normalizedPoint = Vector2.ClampMagnitude(normalizedPoint, 1f);

        // y축 반전 (필요한 경우)
        //normalizedPoint.y = -normalizedPoint.y;

        Debug.Log("Normalized Hit Point: " + normalizedPoint);

        // CueController에 타격점 전달
        cueController.SetHitPoint(normalizedPoint);
    }
}
