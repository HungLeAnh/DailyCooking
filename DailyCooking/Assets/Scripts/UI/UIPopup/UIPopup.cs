using UnityEngine;

public class UIPopup : MonoBehaviour
{
    public virtual void SetupPopup()
    {
        Debug.Log("SetPopup");
    }

    public virtual void HidePopup()
    {
        Debug.Log("HidePopup");
    }

    public virtual void ShowPopup()
    {
        Debug.Log("ShowPopup");
    }
}