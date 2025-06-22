using UnityEngine;
using UnityEngine.UI;

public class ProgressBarUI : MonoBehaviour
{
    [SerializeField] private Image _barImage;

    private void Start()
    { 
        _barImage.fillAmount = 0f;
        Hide();
    }

    public void OnProgressChanged(float progressNormalized)
    {
        _barImage.fillAmount = progressNormalized;
        if (progressNormalized == 0f || progressNormalized >= 1f)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }
    private void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
