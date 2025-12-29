
using DG.Tweening;
using TMPro;
using UnityEngine;

public class UIAlert : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveDistance = 100f;
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private float scaleInitial = 0.5f;

    [SerializeField] private TextMeshProUGUI textElement;
    [SerializeField] private CanvasGroup canvasGroup;
    public void StartAlert(string message)
    {
        textElement.text = message;
        canvasGroup.alpha = 0;
        transform.localScale = Vector3.one * scaleInitial;

        Sequence alertSequence = DOTween.Sequence();
        alertSequence.OnPlay(() => gameObject.SetActive(true));
        alertSequence.Join(transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
        alertSequence.Join(canvasGroup.DOFade(1f, 0.15f));
        alertSequence.Append(transform.DOMoveY(transform.position.y + moveDistance, duration).SetEase(Ease.OutQuad));
        alertSequence.Insert(duration * 0.5f, canvasGroup.DOFade(0f, duration * 0.5f));
        alertSequence.OnComplete(() => gameObject.SetActive(false));
    }
}