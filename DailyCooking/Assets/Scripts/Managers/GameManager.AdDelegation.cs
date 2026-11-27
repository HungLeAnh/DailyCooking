using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// The host's ads, watched by a client so the restaurant keeps running (spec §4):
// - rewarded: the host picks a client each time; the client accepts (15 s) or declines; when no
//   one is left to ask the host may force a random client that has an ad (switchable);
// - interstitial: the host's Break_Time rotates through the clients; the host's Level_Up is skipped.
public partial class GameManager
{
    private const float AD_STATE_REPORT_SECONDS = 5f;
    private const float FORCED_NOTICE_SECONDS = 3f;
    private const float INTERSTITIAL_SAFE_WAIT_SECONDS = 60f;
    private const string HOST_AD_NOTICE = "The host is watching an ad — the restaurant is paused";
    private const string FORCED_NOTICE = "Nobody watched — ad starts in 3 s";
    private const string AD_LOG = "[Net] ads: ";

    private readonly AdDelegationRules adRules = new AdDelegationRules();
    private readonly System.Random adRandom = new System.Random();
    private readonly Dictionary<ulong, bool> clientHasRewarded = new Dictionary<ulong, bool>();
    private readonly Dictionary<ulong, float> clientLastInterstitialAt = new Dictionary<ulong, float>();
    private readonly Dictionary<int, ShopItemType> adRequestRewards = new Dictionary<int, ShopItemType>();
    private int openAdRequestId = -1;
    private ulong openAdRequestClient;
    private ulong? lastAdPick;
    private ShopItemType pendingHostReward;
    private bool interstitialRetried;
    private float nextAdStateReportAt;
    // Client: the ad prompts/coroutines of the current session, torn down when it ends or drops.
    private Coroutine adConsentRoutine;
    private Coroutine adForcedRoutine;
    private Coroutine adInterstitialRoutine;
    private bool adConsentPopupOpen;
    private AdDelegationConfigSO adDelegationConfig;
    private static float localLastInterstitialAt = float.NegativeInfinity;

    // Clients: true while the host's "watching an ad" notice is up (the staleness banner stays out of its way).
    public static bool HostAdNoticeShown { get; private set; }

    public static void NoteLocalInterstitialShown() => localLastInterstitialAt = Time.unscaledTime;

    private bool ForceWatchEnabled
    {
        get
        {
            if (adDelegationConfig == null)
                adDelegationConfig = Resources.Load<AdDelegationConfigSO>("AdDelegationConfig");
            return adDelegationConfig == null || adDelegationConfig.ForceWatchWhenAllDecline;
        }
    }

    // Host with other players in the room: its ads go to a client.
    public bool HostShouldDelegateInterstitials =>
        IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsIds.Count > 1
        && (MultiplayerManager.Instance == null || !MultiplayerManager.Instance.IsSinglePlayerMode);

    #region Host: rewarded
    // The host tapped a daily-free reward. True when a client will watch it (the popup took over).
    public bool RequestRewardedFor(ShopItemType reward)
    {
        if (!HostShouldDelegateInterstitials) return false;
        pendingHostReward = reward;
        ShowAdWatcherPopup(null);
        return true;
    }

    private void ShowAdWatcherPopup(string status)
    {
        float now = Time.unscaledTime;
        List<ulong> clients = OtherClients();
        bool waiting = adRules.HasOpenRequest(now);
        var param = new UIAdWatcherPopup.Param
        {
            Title = "Who watches?",
            Status = waiting ? $"Waiting for {NameOf(openAdRequestClient)}…" : status,
            OnCancel = CancelAdWatcher,
        };
        clients.Sort((a, b) => (lastAdPick == a ? 0 : 1).CompareTo(lastAdPick == b ? 0 : 1));
        foreach (ulong client in clients)
        {
            string reason = PlayerStateMachine.FindForClient(client) == null ? "away"
                : !adRules.CanAsk(client, now) ? "declined"
                : !ClientHasRewarded(client) ? "no ad" : null;
            ulong target = client;
            param.Options.Add(new UIAdWatcherPopup.Option
            {
                Label = NameOf(client) + (reason != null ? $" ({reason})" : lastAdPick == client ? " ✓" : ""),
                Enabled = !waiting && reason == null,
                OnPick = () => AskClientToWatch(target),
            });
        }
        if (!waiting && adRules.CanForce(clients, CanBeForced, now, ForceWatchEnabled))
            param.Options.Add(new UIAdWatcherPopup.Option { Label = "Force a random player", OnPick = ForceRandomWatcher });
        param.Options.Add(new UIAdWatcherPopup.Option { Label = "Me — the restaurant pauses", Enabled = !waiting, OnPick = HostWatchesItself });
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIAdWatcherPopup, param);
    }

    private void AskClientToWatch(ulong client)
    {
        if (!adRules.TryOpen(client, (int)pendingHostReward, Time.unscaledTime, out int requestId))
        {
            ShowAdWatcherPopup($"{NameOf(client)} can't watch right now");
            return;
        }
        OpenAdRequest(requestId, client, pendingHostReward);
        lastAdPick = client;
        Debug.Log(AD_LOG + $"asked {NameOf(client)} (request {requestId})");
        AskWatchClientRpc(requestId, RewardAmount(pendingHostReward), false, RpcTarget.Single(client, RpcTargetUse.Temp));
        ShowAdWatcherPopup(null);
    }

    private void ForceRandomWatcher()
    {
        if (!adRules.TryForce(OtherClients(), CanBeForced, (int)pendingHostReward, adRandom, Time.unscaledTime,
                out ulong client, out int requestId))
        {
            ShowAdWatcherPopup("Nobody can watch right now");
            return;
        }
        OpenAdRequest(requestId, client, pendingHostReward);
        Debug.Log(AD_LOG + $"forced {NameOf(client)} (request {requestId})");
        AskWatchClientRpc(requestId, RewardAmount(pendingHostReward), true, RpcTarget.Single(client, RpcTargetUse.Temp));
        ShowAdWatcherPopup(null);
    }

    // The host watches it itself: everyone sees the pause notice while its phone is in the ad.
    private void HostWatchesItself()
    {
        adRules.EndChain();
        ShopItemType reward = pendingHostReward;
        HostAdPauseClientRpc(true);
        AdProviders.Current.ShowRewarded(reward.ToString(), outcome =>
        {
            HostAdPauseClientRpc(false);
            if (outcome == AdOutcome.Completed)
            {
                if (ServerGrantDailyFree(reward, NetworkManager.ServerClientId, out int amount))
                    UIManager.Instance.ShowAlertMessage($"+{amount}");
            }
            else
            {
                UIManager.Instance.ShowAlertMessage("Ads is Unavailable");
            }
        });
    }

    private void OpenAdRequest(int requestId, ulong client, ShopItemType reward)
    {
        openAdRequestId = requestId;
        openAdRequestClient = client;
        adRequestRewards[requestId] = reward;
    }

    [Rpc(SendTo.Server)]
    private void ReportAdServerRpc(int requestId, AdOutcome outcome, RpcParams rpcParams = default)
    {
        HandleAdReport(requestId, rpcParams.Receive.SenderClientId, outcome);
    }

    private void HandleAdReport(int requestId, ulong sender, AdOutcome outcome)
    {
        if (!adRequestRewards.TryGetValue(requestId, out ShopItemType reward)) return;
        AdDelegationRules.AdResult result = adRules.Report(requestId, sender, outcome, Time.unscaledTime);
        Debug.Log(AD_LOG + $"request {requestId} from {NameOf(sender)}: {outcome} -> {result}");
        if (result == AdDelegationRules.AdResult.Rejected) return;
        adRequestRewards.Remove(requestId);
        if (openAdRequestId == requestId) openAdRequestId = -1;

        string name = NameOf(sender);
        switch (result)
        {
            case AdDelegationRules.AdResult.Grant:
                UIPopupManager.Instance.HidePopup(UIPopupType.UIAdWatcherPopup);
                if (ServerGrantDailyFree(reward, sender, out int amount))
                {
                    string message = $"{name} watched — +{amount}";
                    UIManager.Instance.ShowAlertMessage(message);
                    AlertClient(sender, message);
                }
                else
                {
                    UIManager.Instance.ShowAlertMessage("No free rewards left today.");
                }
                break;
            case AdDelegationRules.AdResult.Reopen:
                ShowAdWatcherPopup(outcome == AdOutcome.FailedOrClosedEarly
                    ? $"{name} didn't finish the ad" : $"{name} can't watch right now");
                break;
            case AdDelegationRules.AdResult.Ended:
                UIPopupManager.Instance.HidePopup(UIPopupType.UIAdWatcherPopup);
                UIManager.Instance.ShowAlertMessage($"{name} didn't finish the ad");
                break;
        }
    }
    #endregion

    #region Client: rewarded
    [Rpc(SendTo.SpecifiedInParams)]
    private void AskWatchClientRpc(int requestId, int amount, bool forced, RpcParams rpcParams = default)
    {
        if (!InPlayingSession()) return;
        if (forced)
            adForcedRoutine = StartCoroutine(ForcedWatch(requestId));
        else
            adConsentRoutine = StartCoroutine(AskConsent(requestId, amount));
    }

    private IEnumerator AskConsent(int requestId, int amount)
    {
        bool answered = false;
        adConsentPopupOpen = true;
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameConfirmPopup, new UIGameConfirmPopup.Param
        {
            Title = "Watch an ad?",
            Message = $"The host asks you to watch an ad for +{amount}",
            YesAction = () =>
            {
                answered = true;
                adConsentPopupOpen = false;
                if (InPlayingSession()) AdAcceptedServerRpc(requestId);
                WatchAndReport(requestId);
            },
            NoAction = () => { answered = true; adConsentPopupOpen = false; ReportAdIfPlaying(requestId, AdOutcome.Declined); },
        });
        float until = Time.unscaledTime + AdDelegationRules.ConsentSeconds;
        while (!answered && Time.unscaledTime < until)
            yield return null;
        adConsentRoutine = null;
        if (!answered)
        {
            if (adConsentPopupOpen)
                UIPopupManager.Instance.HidePopup(UIPopupType.UIGameConfirmPopup);
            adConsentPopupOpen = false;
            ReportAdIfPlaying(requestId, AdOutcome.Timeout);
        }
    }

    private IEnumerator ForcedWatch(int requestId)
    {
        ReconnectingOverlay.SetVisible(true, FORCED_NOTICE);
        yield return new WaitForSecondsRealtime(FORCED_NOTICE_SECONDS);
        ReconnectingOverlay.SetVisible(false);
        adForcedRoutine = null;
        WatchAndReport(requestId);
    }

    // Only for the session that asked: never an ad (that rewards nobody) after it ended or dropped.
    private void WatchAndReport(int requestId)
    {
        if (!InPlayingSession()) return;
        IAdProvider ads = AdProviders.Current;
        if (!ads.HasRewarded)
        {
            ReportAdServerRpc(requestId, AdOutcome.NoAd);
            return;
        }
        ads.ShowRewarded("Host_Reward", outcome => ReportAdIfPlaying(requestId, outcome));
    }

    private void ReportAdIfPlaying(int requestId, AdOutcome outcome)
    {
        if (InPlayingSession()) ReportAdServerRpc(requestId, outcome);
    }

    private bool InPlayingSession() =>
        IsSpawned && MultiplayerManager.Instance != null && MultiplayerManager.Instance.Session.State == SessionState.InGame;

    [Rpc(SendTo.NotServer)]
    private void HostAdPauseClientRpc(bool paused)
    {
        HostAdNoticeShown = paused;
        ReconnectingOverlay.SetVisible(paused, HOST_AD_NOTICE);
    }
    #endregion

    #region Interstitials
    // Server: the host's Break_Time goes to the next client in the rotation instead of the host.
    public void SendInterstitialToClient()
    {
        interstitialRetried = false;
        SendInterstitialToNext(null);
    }

    private void SendInterstitialToNext(ulong? exclude)
    {
        ulong? target = adRules.NextInterstitialTarget(OtherClients(), lastAdPick, LastInterstitialAt, Time.unscaledTime);
        if (!target.HasValue || target == exclude)
        {
            Debug.Log(AD_LOG + "host interstitial skipped (no client can show it)");
            return;
        }
        Debug.Log(AD_LOG + $"host interstitial -> {NameOf(target.Value)}");
        ShowInterstitialClientRpc(RpcTarget.Single(target.Value, RpcTargetUse.Temp));
    }

    private float LastInterstitialAt(ulong client) =>
        clientLastInterstitialAt.TryGetValue(client, out float at) ? at : float.NegativeInfinity;

    [Rpc(SendTo.SpecifiedInParams)]
    private void ShowInterstitialClientRpc(RpcParams rpcParams = default)
    {
        if (!InPlayingSession()) return;
        adInterstitialRoutine = StartCoroutine(ShowInterstitialWhenSafe());
    }

    // Client: at a safe moment (no popup, not in a cooking close-up, 90 s since its last one), within 60 s.
    private IEnumerator ShowInterstitialWhenSafe()
    {
        float until = Time.unscaledTime + INTERSTITIAL_SAFE_WAIT_SECONDS;
        while (Time.unscaledTime < until && !IsSafeForInterstitial())
            yield return null;
        adInterstitialRoutine = null;
        IAdProvider ads = AdProviders.Current;
        if (!InPlayingSession()) yield break;
        if (!IsSafeForInterstitial() || !ads.HasInterstitial)
        {
            InterstitialResultServerRpc(false);
            yield break;
        }
        ads.ShowInterstitial("Break_Time_Host", outcome =>
        {
            NoteLocalInterstitialShown();
            if (InPlayingSession()) InterstitialResultServerRpc(outcome == AdOutcome.Completed);
        });
    }

    private static bool IsSafeForInterstitial()
    {
        if (UIPopupManager.Instance != null && UIPopupManager.Instance.IsShowingPopup()) return false;
        if (CameraModeController.Instance != null && CameraModeController.Instance.Mode == CameraMode.CloseUp) return false;
        return Time.unscaledTime - localLastInterstitialAt >= AdDelegationRules.InterstitialGapSeconds;
    }

    [Rpc(SendTo.Server)]
    private void InterstitialResultServerRpc(bool shown, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (shown)
        {
            clientLastInterstitialAt[sender] = Time.unscaledTime;
            return;
        }
        if (interstitialRetried) return;
        interstitialRetried = true;
        SendInterstitialToNext(sender);
    }
    #endregion

    #region Bookkeeping
    // Called from Update: clients tell the host whether they have an ad and when they last showed one.
    private void TickAdDelegation()
    {
        if (IsSpawned && IsServer)
        {
            // The asked client went quiet: stop waiting after 20 s, as if it had not answered.
            if (adRules.TryGetOverdue(Time.unscaledTime, out int overdueId, out ulong overdueClient))
                HandleAdReport(overdueId, overdueClient, AdOutcome.Timeout);
            return;
        }
        if (!IsSpawned || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;
        if (Time.unscaledTime < nextAdStateReportAt) return;
        nextAdStateReportAt = Time.unscaledTime + AD_STATE_REPORT_SECONDS;
        float since = Mathf.Min(Time.unscaledTime - localLastInterstitialAt, 100000f);
        ReportAdStateServerRpc(AdProviders.Current.HasRewarded, since);
    }

    [Rpc(SendTo.Server)]
    private void ReportAdStateServerRpc(bool hasRewarded, float secondsSinceInterstitial, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        clientHasRewarded[sender] = hasRewarded;
        clientLastInterstitialAt[sender] = Time.unscaledTime - secondsSinceInterstitial;
    }

    private void SubscribeAdEvents()
    {
        UnsubscribeAdEvents();
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += AdDelegation_OnClientDisconnect;
        if (MultiplayerManager.Instance != null)
            MultiplayerManager.Instance.Session.OnChanged += AdDelegation_OnSessionChanged;
    }

    private void UnsubscribeAdEvents()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= AdDelegation_OnClientDisconnect;
        if (MultiplayerManager.Instance != null)
            MultiplayerManager.Instance.Session.OnChanged -= AdDelegation_OnSessionChanged;
    }

    // The client asked to watch left: as if it refused; the host picks again.
    private void AdDelegation_OnClientDisconnect(ulong clientId)
    {
        clientHasRewarded.Remove(clientId);
        clientLastInterstitialAt.Remove(clientId);
        if (openAdRequestId >= 0 && openAdRequestClient == clientId)
            HandleAdReport(openAdRequestId, clientId, AdOutcome.Left);
    }

    private void AdDelegation_OnSessionChanged(SessionState state)
    {
        if (state != SessionState.Ended && state != SessionState.Reconnecting) return;
        TearDownAdPrompts();
        if (state != SessionState.Ended) return;
        adRules.CancelAll();
        adRequestRewards.Clear();
        clientHasRewarded.Clear();
        clientLastInterstitialAt.Clear();
        openAdRequestId = -1;
        lastAdPick = null;
        HostAdNoticeShown = false;
    }

    // The session ended or dropped: no popup, prompt or pending ad of it may outlive it.
    private void TearDownAdPrompts()
    {
        if (adConsentRoutine != null) StopCoroutine(adConsentRoutine);
        if (adForcedRoutine != null) StopCoroutine(adForcedRoutine);
        if (adInterstitialRoutine != null) StopCoroutine(adInterstitialRoutine);
        adConsentRoutine = adForcedRoutine = adInterstitialRoutine = null;
        if (UIPopupManager.Instance != null)
        {
            if (adConsentPopupOpen) UIPopupManager.Instance.HidePopup(UIPopupType.UIGameConfirmPopup);
            UIPopupManager.Instance.HidePopup(UIPopupType.UIAdWatcherPopup);
        }
        adConsentPopupOpen = false;
        if (HostAdNoticeShown) ReconnectingOverlay.SetVisible(false);
        HostAdNoticeShown = false;
    }

    // Host: Cancel in "Who watches?". While waiting it withdraws the request (a late answer grants nothing).
    private void CancelAdWatcher()
    {
        if (!adRules.HasOpenRequest(Time.unscaledTime))
        {
            adRules.EndChain();
            return;
        }
        adRules.CancelOpen();
        adRequestRewards.Remove(openAdRequestId);
        Debug.Log(AD_LOG + $"request {openAdRequestId} cancelled by the host");
        openAdRequestId = -1;
        UIManager.Instance.ShowAlertMessage("Request cancelled");
    }

    [Rpc(SendTo.Server)]
    private void AdAcceptedServerRpc(int requestId, RpcParams rpcParams = default)
    {
        adRules.MarkAccepted(requestId, rpcParams.Receive.SenderClientId);
    }

    private bool CanBeForced(ulong client) =>
        AdCandidates.CanWatch(ClientHasRewarded(client), PlayerStateMachine.FindForClient(client) == null);

    private bool ClientHasRewarded(ulong client) => clientHasRewarded.TryGetValue(client, out bool has) && has;

    private static List<ulong> OtherClients()
    {
        var clients = new List<ulong>();
        if (NetworkManager.Singleton == null) return clients;
        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (id != NetworkManager.ServerClientId) clients.Add(id);
        }
        return clients;
    }

    private static string NameOf(ulong clientId)
    {
        string name = MultiplayerManager.Instance != null
            ? MultiplayerManager.Instance.GetPlayerDataFromClientId(clientId).playerName.ToString() : null;
        return string.IsNullOrEmpty(name) ? $"Player {clientId}" : name;
    }

    private static int RewardAmount(ShopItemType reward)
    {
        DailyFreeCurrency offer = ShopManager.Instance != null ? ShopManager.Instance.DailyFreeCurrency.Find(x => x.Id == reward) : null;
        return offer != null && int.TryParse(offer.Reward, out int amount) ? amount : 0;
    }
    #endregion
}
