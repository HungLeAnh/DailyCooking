using UnityEngine;

// Switches for the host's ads watched by clients, changeable without code.
[CreateAssetMenu(fileName = "AdDelegationConfig", menuName = "Ads/Ad Delegation Config")]
public class AdDelegationConfigSO : ScriptableObject
{
    [Tooltip("When every client refused, the host may force a random client with an ad loaded to watch " +
             "a rewarded ad. Rewarded ads are normally opt-in: the owner accepted the ad-policy risk.")]
    public bool ForceWatchWhenAllDecline = true;
}
