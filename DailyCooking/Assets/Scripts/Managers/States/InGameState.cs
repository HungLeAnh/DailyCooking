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
    public override void Update()
    {
        if (interstitialCounter > 0)
        {
            interstitialCounter -= Time.deltaTime;
        }
        else
        {
            interstitialCounter = interstitialInterval;
            AdsManager.Instance.ShowInterstitialAds();
        }
    }
    public override void Exit()
    {
    }
}
