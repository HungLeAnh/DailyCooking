using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameState : GameManagerBaseState
{
    private float interstitialCounter = 0;
    private float interstitialInterval = 300f;
    private int interstitialLevelUnlock = 2;
    public InGameState(GameManager gameManager) : base(gameManager) { }

    public override void Enter()
    {
        interstitialCounter = interstitialInterval;

        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if(string.Equals(arg0.name,Loader.Scene.GameScene.ToString(), 
            StringComparison.OrdinalIgnoreCase))
        {
            interstitialCounter = interstitialInterval;
            //GameManager.Instance.InitializePlayer();
        }
    }

    public override async void Update()
    {
        if(gameManager.GameData == null)
            return;
        if(gameManager.GameData.RestaurantData.Level < interstitialLevelUnlock)
            return;
        if (interstitialCounter > 0)
        {
            interstitialCounter -= Time.deltaTime;
            return;
        }

        while (UIPopupManager.Instance.IsShowingPopup())
            await Task.Yield(); 
        
        interstitialCounter = interstitialInterval;
        AdsManager.Instance.ShowInterstitialAds();
        
    }
    public override void Exit()
    {
        SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

    }
}
