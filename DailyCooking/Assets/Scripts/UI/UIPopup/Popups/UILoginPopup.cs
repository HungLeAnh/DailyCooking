using Unity.Services.Authentication.PlayerAccounts;
using UnityEngine;
using UnityEngine.UI;

public class UILoginPopup : UIPopup
{
    [SerializeField] private Button closeLoginPanelButton;
    [SerializeField] private Button loginAnonymously;
    [SerializeField] private UILinkButton linkGooglePlay;
    [SerializeField] private UILinkButton linkUnity;
    private void Awake()
    {
        closeLoginPanelButton.onClick.AddListener(() => 
        { 
            HidePopup();
        });
        loginAnonymously.onClick.AddListener(async () =>
        {
            await SessionManager.Instance.SignInAnonymouslyAsync();
        });
        SessionManager.Instance.OnGoogleLinkOrUnlink += linkGooglePlay.OnLinkOrUnlink;
        linkGooglePlay.SetLinkedState(AuthenticationType.GooglePlayGames, 
        () =>
        {
            SessionManager.Instance.StartSignInWithGooglePlayGames();
        },
        async () => 
        { 
            await SessionManager.Instance.UnlinkGooglePlayGamesAsync();
        });
        SessionManager.Instance.OnUnityLinkOrUnlink += linkUnity.OnLinkOrUnlink;
        linkUnity.SetLinkedState(AuthenticationType.Unity, 
        () =>
        {
            SessionManager.Instance.SignInOrLinkWithUnity();
        },
        async () => 
        { 
            await SessionManager.Instance.UnlinkUnityAsync();
        });
    }
    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
    }
    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
    }
    public override void SetupPopup()
    {
        base.SetupPopup();
    }
}
