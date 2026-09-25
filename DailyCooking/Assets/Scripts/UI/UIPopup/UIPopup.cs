using UnityEngine;

public class UIPopup : MonoBehaviour
{
    protected object _closeParam;
    protected object _openParam;
    public virtual void SetupPopup()
    {
        //Debug.Log("SetPopup");
        var rectTransform = this.gameObject.transform as RectTransform;
        rectTransform.SetAsLastSibling();
    }

    public virtual void HidePopup(object param = null)
    {
        //Debug.Log("HidePopup");
        gameObject.SetActive(false);
        _closeParam = param;
        UIPopupManager.Instance.RemoveFromeVisibleList(this);
    }

    public virtual void ShowPopup(object param = null)
    {
        //Debug.Log("ShowPopup");
        gameObject.SetActive(true);
        // Popups are created once and reused: bring a re-shown popup in front of the others.
        transform.SetAsLastSibling();
        _openParam = param;
    }
}