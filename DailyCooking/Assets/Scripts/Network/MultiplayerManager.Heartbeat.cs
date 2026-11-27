using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// A 1 s beat each way. 5 s without one: the client shows "Reconnecting…", the host marks that
// player away. Long before the 60 s transport timeout decides the connection is gone.
public partial class MultiplayerManager
{
    private const float BEAT_INTERVAL_SECONDS = 1f;

    private readonly StalenessTracker hostSilence = new StalenessTracker();
    private readonly Dictionary<ulong, StalenessTracker> clientSilence = new Dictionary<ulong, StalenessTracker>();
    private readonly HashSet<ulong> quietClients = new HashSet<ulong>();
    private readonly List<ulong> beatScratch = new List<ulong>();
    private float nextBeatAt;
    private float nextSeatCheckAt;
    private bool hostLooksStale;

    // Server: a quiet or dropped player (by player id). Back = heard again while still connected.
    public event System.Action<string> OnPlayerAway;
    public event System.Action<string> OnPlayerBack;
    public event System.Action<string> OnSeatExpired;

    public bool IsHostStale => hostLooksStale;

    private void Update()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
        {
            SetHostStale(false);
            return;
        }
        float now = Time.unscaledTime;
        if (now >= nextBeatAt)
        {
            nextBeatAt = now + BEAT_INTERVAL_SECONDS;
            if (nm.IsServer) BeatClientRpc();
            else if (nm.IsConnectedClient) BeatServerRpc();
        }
        if (nm.IsServer)
        {
            CheckQuietClients(now);
            if (now >= nextSeatCheckAt)
            {
                nextSeatCheckAt = now + BEAT_INTERVAL_SECONDS;
                foreach (string playerId in Seats.Expire(now))
                {
                    Debug.Log(LOG + $"seat expired for {playerId}");
                    OnSeatExpired?.Invoke(playerId);
                }
            }
        }
        else
        {
            SetHostStale(Session.State == SessionState.InGame && hostSilence.IsStale(now));
        }
    }

    [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
    private void BeatClientRpc() => hostSilence.Heard(Time.unscaledTime);

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
    private void BeatServerRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (clientSilence.TryGetValue(sender, out StalenessTracker tracker))
            tracker.Heard(Time.unscaledTime);
    }

    private void StartHearing(ulong clientId)
    {
        var tracker = new StalenessTracker();
        tracker.Reset(Time.unscaledTime);
        clientSilence[clientId] = tracker;
        quietClients.Remove(clientId);
    }

    private void StopHearing(ulong clientId)
    {
        clientSilence.Remove(clientId);
        quietClients.Remove(clientId);
    }

    private void CheckQuietClients(float now)
    {
        beatScratch.Clear();
        beatScratch.AddRange(clientSilence.Keys);
        foreach (ulong clientId in beatScratch)
        {
            if (clientId == NetworkManager.ServerClientId) continue;
            bool quiet = clientSilence[clientId].IsStale(now);
            string playerId = PlayerIdFor(clientId);
            if (quiet && quietClients.Add(clientId))
            {
                Debug.Log(LOG + $"client quiet: {clientId} ({playerId})");
                if (playerId != null) OnPlayerAway?.Invoke(playerId);
            }
            else if (!quiet && quietClients.Remove(clientId))
            {
                Debug.Log(LOG + $"client back: {clientId} ({playerId})");
                if (playerId != null) OnPlayerBack?.Invoke(playerId);
            }
        }
    }

    private void SetHostStale(bool stale)
    {
        if (stale == hostLooksStale) return;
        hostLooksStale = stale;
        if (stale) Debug.Log(LOG + "no word from the host for 5 s");
        if (Session.State != SessionState.Reconnecting)
            ReconnectingOverlay.SetVisible(stale);
    }

    private void ResetHeartbeat()
    {
        hostSilence.Reset(Time.unscaledTime);
        clientSilence.Clear();
        quietClients.Clear();
        SetHostStale(false);
    }
}
