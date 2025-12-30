using UnityEngine;

public class UIPopup : MonoBehaviour
{
    protected object _closeParam;
    protected object _openParam;
    public virtual void SetupPopup()
    {
        //Debug.Log("SetPopup");
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
        _openParam = param;
    }
}