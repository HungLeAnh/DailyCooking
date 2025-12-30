using System.Threading.Tasks;
using UnityEngine;

public class InGameState : GameManagerBaseState
{
    private float interstitialCounter = 0;
    private float interstitialInterval = 30f;
    public InGameState(GameManager gameManager) : base(gameManager) { }

    public override void Enter()
    {
        GameManager.Instance.InitializePlayer();
        interstitialCounter = interstitialInterval;
    }
    public override async void Update()
    {
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
    }
}
