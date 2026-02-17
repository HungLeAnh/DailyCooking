using System.Threading.Tasks;
using UnityEngine;

public class InGameState : GameManagerBaseState
{
    private float interstitialCounter = 0;
    private float interstitialInterval = 300f;
    private int interstitialLevelUnlock = 2;
    public InGameState(GameManager gameManager) : base(gameManager) { }

    public override void Enter()
    {
        GameManager.Instance.InitializePlayer();
        interstitialCounter = interstitialInterval;
    }
    public override async void Update()
    {
        if(GameManager.Instance.GameData.PlayerStats.playerData.Level < interstitialLevelUnlock)
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
    }
}
