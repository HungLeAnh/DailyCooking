using UnityEngine;

public class BurnWarningUI : MonoBehaviour
{
    private bool _isVisible;

    private void Start()
    {
        Hide();
    }

    public void OnProgressChanged(IHasProgress sender, float progressNormalized)
    {
        float burnShowProgressAmount = .5f;

        bool show = sender.IsDone() && progressNormalized >= burnShowProgressAmount;

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
