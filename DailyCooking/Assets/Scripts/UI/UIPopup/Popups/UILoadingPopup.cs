using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILoadingPopup : UIPopup
{
    [SerializeField] private LoaderCallBack _loadingCallback;

    public override void ShowPopup()
    {
        base.ShowPopup();
        _loadingCallback.UpdateCallBack();
    }
}
