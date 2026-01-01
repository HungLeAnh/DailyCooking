using DG.Tweening;
using System.Net;
using UnityEngine;

public class DOTweenHandClick : MonoBehaviour
{
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private GameObject handGameObejct;

    public float moveDistance = 50f;
    public float duration = 2f;
    [Header("Settings")]
    public float dragDuration = 1.5f;
    public float clickScale = 0.8f;

    private Sequence dragSequence;
    private void Start()
    {
        // Move the image up and down relative to its current position
        transform.DOLocalMoveY(transform.localPosition.y + moveDistance, duration)
            .SetEase(Ease.InOutSine) 
            .SetLoops(-1, LoopType.Yoyo);
    }
    public void SetHandDrag(Vector3 startPosition, Vector3 endPosition)
    {
        var handIcon = handGameObejct.transform as RectTransform;

        CanvasGroup canvasGroup = handIcon.GetComponent<CanvasGroup>();

        dragSequence?.Kill();

        // 1. Initial State
        handIcon.anchoredPosition = startPosition;
        handIcon.localScale = Vector3.one;
        if (canvasGroup != null) canvasGroup.alpha = 0;

        dragSequence = DOTween.Sequence();

        // 2. Fade In & Press
        dragSequence.Append(canvasGroup.DOFade(1f, 0.2f));
        dragSequence.Append(handIcon.DOScale(clickScale, 0.15f).SetEase(Ease.OutQuad));

        // 3. The Drag (Moving to the Vector3 endPosition)
        // We use DOAnchorPos because we are providing UI local coordinates
        dragSequence.Append(handIcon.DOAnchorPos(endPosition, dragDuration)
            .SetEase(Ease.InOutQuad));

        // 4. Release & Fade Out
        dragSequence.Append(handIcon.DOScale(1f, 0.15f).SetEase(Ease.InQuad));
        dragSequence.Append(canvasGroup.DOFade(0f, 0.2f));

        // 5. Reset and Loop
        dragSequence.AppendInterval(0.5f);
        dragSequence.OnStepComplete(() => handIcon.anchoredPosition = startPosition);
        dragSequence.SetLoops(-1);
    }
    public void SetHandDrag3D(Transform worldStartTransform, Vector3 worldEndPos)
    {
        var handIcon = handGameObejct.transform as RectTransform;
        CanvasGroup canvasGroup = handIcon.GetComponent<CanvasGroup>();

        if (canvasGroup != null) canvasGroup.alpha = 0;
        dragSequence?.Kill();

        dragSequence = DOTween.Sequence();

        float dragProgress = 0;

        dragSequence.AppendCallback(() => {
            dragProgress = 0;
            handIcon.anchoredPosition = GetCanvasPos(worldStartTransform.position);
            handIcon.localScale = Vector3.one;
        });

        dragSequence.Append(canvasGroup.DOFade(1f, 0.3f));
        dragSequence.Append(handIcon.DOScale(0.8f, 0.2f).SetEase(Ease.OutQuad));

        dragSequence.Append(DOTween.To(() => dragProgress, x => dragProgress = x, 1f, dragDuration)
            .SetEase(Ease.InOutQuad)
            .OnUpdate(() => {
                Vector3 currentWorldPos = Vector3.Lerp(worldStartTransform.position, worldEndPos, dragProgress);

                handIcon.anchoredPosition = GetCanvasPos(currentWorldPos);
            }));

        dragSequence.Append(handIcon.DOScale(1f, 0.2f));
        dragSequence.Append(canvasGroup.DOFade(0f, 0.3f));

        dragSequence.SetLoops(-1);
    }
    private Vector2 GetCanvasPos(Vector3 worldPos)
    {
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPos);
        return screenPoint;
    }
}
