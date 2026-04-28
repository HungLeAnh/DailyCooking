using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplayer;
using UnityEngine;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class SessionManager : PersistentSingleton<SessionManager>
{
    ISession activeSession;
    const string playerNamePropertyKey = "PlayerName";
    private string googlePlayGameToken;
    public string Error;
    protected override async void Awake()
    {
        base.Awake();
        try
        {
            PlayGamesPlatform.DebugLogEnabled = true;
            PlayGamesPlatform.Activate();
            SetupEvents();
#if UNITY_EDITOR
            // Simulate a fake success for testing Editor UI/Logic
            LoginGooglePlayGames();
#elif UNITY_ANDROID
            //Initialize PlayGamesPlatform
            PlayGamesPlatform.Activate();
            LoginGooglePlayGames();
#else
            await SignInAnonymouslyAsync();
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
        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log($"Player ID: {AuthenticationService.Instance.PlayerId}");
        };

        AuthenticationService.Instance.SignInFailed += (err) => {
            Debug.LogError($"Sign In Failed: {err}");
        };
    }
    private async Task SignInAnonymouslyAsync()
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
                    // This token serves as an example to be used for SignInWithGooglePlayGames
                });
            }
            else
            {
                Error = "Failed to retrieve Google play games authorization code";
                Debug.Log("Login Unsuccessful");
            }
        });
    }
    async Task SignInWithGooglePlayGamesAsync(string authCode)
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
    private async Task LinkWithGooglePlayGamesAsync(string authCode)
    {
        try
        {
            await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authCode);
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
    private async Task UnlinkGooglePlayGamesAsync()
    {
        try
        {
            await AuthenticationService.Instance.UnlinkGooglePlayGamesAsync();
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
}