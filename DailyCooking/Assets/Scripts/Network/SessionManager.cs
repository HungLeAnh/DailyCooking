using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using Unity.Services.Authentication.PlayerAccounts;

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
    ISession activeSession;
    const string playerNamePropertyKey = "PlayerName";
    private string googlePlayGameToken;

    public string GooglePlayGameToken => googlePlayGameToken;
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
    private async void StartSessionAsHost()
    {
        try
        {
            var playerProperties = await GetPlayerProperties();
            var options = new SessionOptions
            {
                MaxPlayers = 4,
                IsLocked = false,
                IsPrivate = false,
                PlayerProperties = playerProperties,
            }.WithRelayNetwork();

            Debug.Log("Session created successfully as host");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create session as host: {e.Message}");
        }
    }
    private async Task<Dictionary<string,PlayerProperty>> GetPlayerProperties()
    {
        var playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
        var playerNameProperty = new PlayerProperty(playerName,VisibilityPropertyOptions.Member);
        return new Dictionary<string, PlayerProperty>
        {
            { playerNamePropertyKey, playerNameProperty }
        };
    }
    private async Task JoinSessionById(string sessionId)
    {
        activeSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
        Debug.Log($"Session {activeSession.Id} joined");
    }    
    private async Task JoinSessionByCode(string sessionCode)
    {
        activeSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);
        Debug.Log($"Session {activeSession.Id} joined");
    }
    private async Task KickPlayer(string playerId)
    {
        if (!activeSession.IsHost)
            return;

        await activeSession.AsHost().RemovePlayerAsync(playerId);   
        Debug.Log($"Player {playerId} kicked from session {activeSession.Id}");
    }
    private async Task<IList<ISessionInfo>> QuerySessions()
    {
        var sessionsQueryOptions = new QuerySessionsOptions();
        QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(sessionsQueryOptions);
        return results.Sessions;
    }
    private async Task LeaveSession()
    {
        if (activeSession == null)
            return;
        try
        {
            await activeSession.LeaveAsync();
            Debug.Log($"Left session {activeSession.Id}");

        }
        catch (Exception ex)
        {

        }
        finally
        {
            activeSession = null;

        }
    }
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
}