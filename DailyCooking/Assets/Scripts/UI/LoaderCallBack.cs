using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoaderCallBack : MonoBehaviour
{
    [SerializeField] private float delayCallBack = 3f;
    private bool isFirstUpadate = true;

    private void Start()
    {
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
    }
    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        isFirstUpadate = true;

        UIPopupManager.Instance.HidePopup(UIPopupType.UILoadingPopup);
    }

    public void UpdateCallBack()
    {
        if (isFirstUpadate)
        {
            isFirstUpadate = false;

            Loader.LoaderCallback();

            //StartCoroutine(CallBackWithDelay());
        }
    }

    IEnumerator CallBackWithDelay()
    {
        yield return new WaitForSeconds(delayCallBack);            
        isFirstUpadate = true;

        UIPopupManager.Instance.HidePopup(UIPopupType.UILoadingPopup);

    }
}
