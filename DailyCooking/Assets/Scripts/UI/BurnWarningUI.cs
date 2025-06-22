using UnityEngine;

public class BurnWarningUI : MonoBehaviour
{

    private void Start()
    {
        Hide();
    }

    public void OnProgressChanged(IHasProgress sender, float progressNormalized)
    {
        float burnShowProgressAmount = .5f;

        bool show = sender.IsDone() && progressNormalized >= burnShowProgressAmount;

        if (show)
        {
            Show();
        }
        else
        {
            Hide();
        }

    }
    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);

    }

}
