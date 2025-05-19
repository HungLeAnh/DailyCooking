using UnityEngine;

public class UIPopup : MonoBehaviour
{
    private object _param;
    public virtual void SetupPopup()
    {
        //Debug.Log("SetPopup");
    }

    public virtual void HidePopup(object param = null)
    {
        //Debug.Log("HidePopup");
        gameObject.SetActive(false);
        _param = param;
    }

    public virtual void ShowPopup()
    {
        //Debug.Log("ShowPopup");
        gameObject.SetActive(true);
    }
}