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

    public string PlayerId => AuthenticationService.Instance.PlayerId;
    protected override async void Awake()
    {
        base.Awake();
        // A second copy (from reloading MainMenuScene) is being destroyed; it must not
        // initialize services or subscribe the sign-in events again.
        if (Instance != this) return;
        try
        {
            await UnityServices.InitializeAsync();
            //await SignInAnonymouslyAsync();
            SetupEvents();

#if UNITY_ANDROID
            //Initialize PlayGamesPlatform
            PlayGamesPlatform.DebugLogEnabled = Debug.isDebugBuild;
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
    // Startup: silent Google Play Games sign-in, so the account is ready when the player taps
    // "Sign in with Google". No server auth code is requested here: codes are single-use.
    public void LoginGooglePlayGames()
    {
#if UNITY_ANDROID
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            Debug.Log(status == SignInStatus.Success
                ? "Login with Google Play games successful."
                : "Login with Google Play games unsuccessful.");
        });
#else
        Debug.Log("Google Play Games is only available on Android.");
#endif
    }
    public async void StartSignInWithGooglePlayGames()
    {
#if UNITY_ANDROID
        // A fresh code for every attempt: Unity Authentication rejects a code that was used before.
        string authCode = await RequestGooglePlayGamesAuthCodeAsync();
        if (string.IsNullOrEmpty(authCode))
        {
            Debug.LogWarning("Google Play Games sign-in failed or was cancelled.");
            return;
        }
        if (!AuthenticationService.Instance.IsSignedIn)
            await SignInWithGooglePlayGamesAsync(authCode);
        else
            await LinkWithGooglePlayGamesAsync(authCode);
#else
        Debug.Log("Google Play Games is only available on Android.");
        await Task.CompletedTask;
#endif
    }
#if UNITY_ANDROID
    // Signs in to Google Play Games if needed, then requests a server auth code (null on failure).
    private static Task<string> RequestGooglePlayGamesAuthCodeAsync()
    {
        var result = new TaskCompletionSource<string>();
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status != SignInStatus.Success)
            {
                result.TrySetResult(null);
                return;
            }
            PlayGamesPlatform.Instance.RequestServerSideAccess(true, code => result.TrySetResult(code));
        });
        return result.Task;
    }
#endif
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
        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
        unityTransport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
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
        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
        unityTransport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
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