using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class SessionManager : PersistentSingleton<SessionManager>
{
    ISession activeSession;
    const string playerNamePropertyKey = "PlayerName";
    public ISession ActiveSession
    {
        get => activeSession;
        set
        {
            if (activeSession != value)
            {
                activeSession = value;
            }
        }
    }
    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Initialize Session Manager successfully");
        }
        catch(Exception e)
        {
            Debug.LogError($"Failed to initialize Session Manager: {e.Message}");
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
        ActiveSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
        Debug.Log($"Session {ActiveSession.Id} joined");
    }    
    private async Task JoinSessionByCode(string sessionCode)
    {
        ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);
        Debug.Log($"Session {ActiveSession.Id} joined");
    }
    private async Task KickPlayer(string playerId)
    {
        if (!ActiveSession.IsHost)
            return;

        await ActiveSession.AsHost().RemovePlayerAsync(playerId);   
        Debug.Log($"Player {playerId} kicked from session {ActiveSession.Id}");
    }
    private async Task<IList<ISessionInfo>> QuerySessions()
    {
        var sessionsQueryOptions = new QuerySessionsOptions();
        QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(sessionsQueryOptions);
        return results.Sessions;
    }
    private async Task LeaveSession()
    {
        if (ActiveSession == null)
            return;
        try
        {
            await ActiveSession.LeaveAsync();
            Debug.Log($"Left session {ActiveSession.Id}");

        }
        catch (Exception ex)
        {

        }
        finally
        {
            ActiveSession = null;

        }
    }
}
