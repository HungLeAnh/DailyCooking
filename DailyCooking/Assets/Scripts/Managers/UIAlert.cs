
using DG.Tweening;
using TMPro;
using UnityEngine;

public class UIAlert : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveDistance = 100f;
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private float scaleInitial = 0.5f;


    [SerializeField] private GameObject alertObject;
    public void StartAlert(string message)
    {
        var alert = Instantiate(alertObject,gameObject.transform);
        var messageText = alert.GetComponentInChildren<TextMeshProUGUI>();
        messageText.text = message;

        var canvasGroup = alert.GetComponentInChildren<CanvasGroup>();
        canvasGroup.alpha = 0;
        alert.transform.localScale = Vector3.one * scaleInitial;

        Sequence alertSequence = DOTween.Sequence();
        alertSequence.OnStart(() => { alert.SetActive(true); });
        alertSequence.Join(alert.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
        alertSequence.Join(canvasGroup.DOFade(1f, 0.15f));
        alertSequence.Append(alert.transform.DOMoveY(alert.transform.position.y + moveDistance, duration).SetEase(Ease.OutQuad));

        alertSequence.Insert(duration * 0.5f, canvasGroup.DOFade(0f, duration * 0.5f));
        alertSequence.OnComplete(() => Destroy(alert));
    }
}