using UnityEngine;

public class BurnWarningUI : MonoBehaviour
{
    private bool _isVisible;
    private float _showProgressAmount = 0.5f;

    private void Start()
    {
        Hide();
    }

    public void SetThreshold(float progressNormalized)
    {
        _showProgressAmount = Mathf.Clamp01(progressNormalized);
    }

    public void OnProgressChanged(IHasProgress sender, float progressNormalized)
    {
        bool show = sender.IsDone() && progressNormalized >= _showProgressAmount;

        if (show != _isVisible)
        {
            _isVisible = show;
            if (show) Show();
            else Hide();
        }
    }
    public void Show()
    {
        _isVisible = true;
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        _isVisible = false;
        gameObject.SetActive(false);
    }
}
