using UnityEngine;

public class BurnWarningUI : MonoBehaviour
{
    private IHasProgress _IHasProgressGameObject;

    private void Start()
    {
        _IHasProgressGameObject = GetComponentInParent<IHasProgress>();
        if( _IHasProgressGameObject != null)
        {
            _IHasProgressGameObject.OnProgressChanged += IHasProgressGameObject_OnProgressChanged;
        }
        Hide();
    }

    private void IHasProgressGameObject_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e)
    {
        float burnShowProgressAmount = .5f;

        bool show = _IHasProgressGameObject.IsDone() && e.progressNormalized >= burnShowProgressAmount;

        if (show)
        {
            Show();
        }
        else
        {
            Hide();
        }

    }
    private void Show()
    {
        gameObject.SetActive(true);
    }
    private void Hide()
    {
        gameObject.SetActive(false);

    }

}
