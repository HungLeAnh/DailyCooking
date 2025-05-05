using System.Collections;
using UnityEngine;

public class LoaderCallBack : MonoBehaviour
{
    [SerializeField] private float delayCallBack = 3f;
    private bool isFirstUpadate = true;
    
    public void UpdateCallBack()
    {
        if (isFirstUpadate)
        {
            isFirstUpadate = false;

            Loader.LoaderCallback();

            StartCoroutine(CallBackWithDelay());
        }
    }

    IEnumerator CallBackWithDelay()
    {
        yield return new WaitForSeconds(delayCallBack);            
        isFirstUpadate = true;

        UIPopupManager.Instance.HidePopup(UIPopupType.UILoadingPopup.ToString());

    }
}
