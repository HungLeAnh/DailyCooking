using UnityEngine;
using UnityEngine.UI;

public class ProgressBarUI : MonoBehaviour
{
    [SerializeField] private Image _barImage;

    private float _lastFillAmount;
    private bool _isVisible;

    private void Start()
    {
        _barImage.fillAmount = 0f;
        Hide();
    }

    public void OnProgressChanged(float progressNormalized)
    {
        if (!Mathf.Approximately(_lastFillAmount, progressNormalized))
        {
            _lastFillAmount = progressNormalized;
            _barImage.fillAmount = progressNormalized;
        }

        bool show = progressNormalized > 0f && progressNormalized < 1f;
        if (show != _isVisible)
        {
            _isVisible = show;
            if (show) Show();
            else Hide();
        }
    }
    private void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        _isVisible = false;
        gameObject.SetActive(false);
    }
}
