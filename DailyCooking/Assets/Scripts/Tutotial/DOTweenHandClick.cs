using DG.Tweening;
using UnityEngine;

public class DOTweenHandClick : MonoBehaviour
{
    [SerializeField] private GameObject handGameObejct;

    public float moveDistance = 50f;
    public float duration = 2f;

    private void Start()
    {
        // Move the image up and down relative to its current position
        transform.DOLocalMoveY(transform.localPosition.y + moveDistance, duration)
            .SetEase(Ease.InOutSine) 
            .SetLoops(-1, LoopType.Yoyo);
    }
}
