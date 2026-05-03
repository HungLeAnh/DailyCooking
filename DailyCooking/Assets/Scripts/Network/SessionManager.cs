using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Services.Relay;

using Unity.Netcode.Transports.UTP;


#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public enum AuthenticationType
{
    Anonymous,
    GooglePlayGames,
    Unity,
}

public class SessionManager : PersistentSingleton<SessionManager>
{
    public Action OnGoogleLinkOrUnlink;
    public Action OnUnityLinkOrUnlink;
    const string playerNamePropertyKey = "PlayerName";
    private string googlePlayGameToken;

    public string GooglePlayGameToken => googlePlayGameToken;
    public string PlayerId => AuthenticationService.Instance.PlayerId;
    protected override async void Awake()
    {
        base.Awake();
        try
        {
            await UnityServices.InitializeAsync();
            PlayGamesPlatform.DebugLogEnabled = true;
            //await SignInAnonymouslyAsync();
            SetupEvents();

#if UNITY_ANDROID
            //Initialize PlayGamesPlatform
            PlayGamesPlatform.Activate();
            LoginGooglePlayGames();
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Session Manager: {e.Message}");
        }
    }
    private async void Start()
    {

    }
    private void SetupEvents()
    {
        PlayerAccountService.Instance.SignedIn += SignInWithUnityAuth;
        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log($"Player ID: {AuthenticationService.Instance.PlayerId}");
        };

        AuthenticationService.Instance.SignInFailed += (err) => {
            Debug.LogError($"Sign In Failed: {err}");
        };
    }
    #region Anonymous Sign In
    public async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }
    private bool HasAnonymousID()
    {
        return AuthenticationService.Instance.PlayerId != null;
    }
    #endregion
    #region Google Play Games
    public void LoginGooglePlayGames()
    {
        PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            if (success == SignInStatus.Success)
            {
                Debug.Log("Login with Google Play games successful.");

                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    Debug.Log("Authorization code: " + code);
                    googlePlayGameToken = code;
                });
            }
            else
            {
                Debug.Log("Login Unsuccessful");
            }
        });
    }
    public void StartSignInWithGooglePlayGames()
    {
        if (!PlayGamesPlatform.Instance.IsAuthenticated()||!HasGooglePlayGamesID())
        {
            LoginGooglePlayGames();
        }
        SignInOrLinkWithGooglePlayGames();
    }
    private async void SignInOrLinkWithGooglePlayGames()
    {
        if (string.IsNullOrEmpty(googlePlayGameToken))
        {
            Debug.LogError("Google Play Games token is null or empty. Cannot sign in or link.");
            return;
        }
        if(!AuthenticationService.Instance.IsSignedIn)
        {
            await SignInWithGooglePlayGamesAsync(googlePlayGameToken);
        }
        else
        {
            await LinkWithGooglePlayGamesAsync(googlePlayGameToken);
        }
    }
    public async Task SignInWithGooglePlayGamesAsync(string authCode)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
            Debug.Log("SignIn is successful.");
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }
    public async Task LinkWithGooglePlayGamesAsync(string authCode)
    {
        try
        {
            await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authCode);
            OnGoogleLinkOrUnlink?.Invoke();
            Debug.Log("Link is successful.");
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            // Prompt the player with an error message.
            Debug.LogError("This user is already linked with another account. Log in instead.");

        }

        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }
    public async Task UnlinkGooglePlayGamesAsync()
    {
        try
        {
            await AuthenticationService.Instance.UnlinkGooglePlayGamesAsync();
            OnGoogleLinkOrUnlink?.Invoke();
            Debug.Log("Unlink is successful.");
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }
    private bool HasGooglePlayGamesID()
    {
        if(AuthenticationService.Instance.PlayerInfo == null)
            return false;
        return AuthenticationService.Instance.PlayerInfo.GetGooglePlayGamesId() != null;
    }
    #endregion
    #region Unity Authentication
    public async void SignInOrLinkWithUnity()
    {
        if (!PlayerAccountService.Instance.IsSignedIn)
        {
            StartPlayerAccountsSignInAsync();
        }
        else
        {
            await LinkWithUnityAsync(PlayerAccountService.Instance.AccessToken);
        }
    }
    public async void StartPlayerAccountsSignInAsync()
    {
        if(AuthenticationService.Instance.IsSignedIn)
        {
            SignInWithUnityAuth();
            return;
        }
        try
        {
            // This will open the system browser and prompt the user to sign in to Unity Player Accounts
            await PlayerAccountService.Instance.StartSignInAsync();            
        }
        catch (PlayerAccountsException ex)
        {
            // Compare error code to PlayerAccountsErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }

    }
    public async void SignInWithUnityAuth()
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
            OnUnityLinkOrUnlink?.Invoke();
            Debug.Log("SignIn is successful.");
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }
    public async Task LinkWithUnityAsync(string accessToken)
    {
        try
        {
            await AuthenticationService.Instance.LinkWithUnityAsync(accessToken);
            Debug.Log("Link is successful.");
            OnUnityLinkOrUnlink?.Invoke();
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            // Prompt the player with an error message.
            Debug.LogError("This user is already linked with another account. Log in instead.");
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }
    public async Task UnlinkUnityAsync()
    {
        try
        {
            await AuthenticationService.Instance.UnlinkUnityAsync();
            OnUnityLinkOrUnlink?.Invoke();
            Debug.Log("Unlink is successful.");
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }
    public void SignOut(bool clearSessionToken = false)
    {
        // Sign out of Unity Authentication, with the option to clear the session token
        AuthenticationService.Instance.SignOut(clearSessionToken);

        // Sign out of Unity Player Accounts
        PlayerAccountService.Instance.SignOut();
    }
    private bool HasUnityID()
    {
        if(AuthenticationService.Instance.PlayerInfo == null)
            return false;
        return AuthenticationService.Instance.PlayerInfo.GetUnityId() != null;
    }
    #endregion
    #region Relay
    public async Task<string> StartHostWithRelay(int maxConnections, string connectionType)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            UIManager.Instance.ShowAlertMessage("You must be signed in to start hosting.");
            return null;
        }
        
        var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }
    public async Task<bool> StartClientWithRelay(string joinCode, string connectionType)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            UIManager.Instance.ShowAlertMessage("You must be signed in to start hosting.");
            return false;
        }

        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
    }
    #endregion
    public bool HasIDOfType(AuthenticationType type)
    {
        switch (type)
        {
            case AuthenticationType.Anonymous:
                return HasAnonymousID();
            case AuthenticationType.GooglePlayGames:
                return HasGooglePlayGamesID();
            case AuthenticationType.Unity:
                return HasUnityID();
            default:
                return false;
        }
    }
    public bool IsSignedIn()
    {
        return AuthenticationService.Instance.IsSignedIn || PlayerAccountService.Instance.IsSignedIn;
    }
}