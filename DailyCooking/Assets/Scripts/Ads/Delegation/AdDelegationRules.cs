using System;
using System.Collections.Generic;

public enum AdOutcome { Completed, Declined, NoAd, Timeout, FailedOrClosedEarly, Left }

// Server-side rules for the host's ads watched by a client: one open rewarded request at a time,
// a 60 s cooldown for whoever refused, a host-triggered force once everyone refused, request expiry,
// and the rotation of the host's interstitials. Pure: AdDelegationService applies the results.
public sealed class AdDelegationRules
{
    public const float ConsentSeconds = 15f;
    public const float AskCooldownSeconds = 60f;
    public const float RequestExpirySeconds = 120f;
    public const float InterstitialGapSeconds = 90f;

    public enum AdResult { Grant, Reopen, Ended, Rejected }

    private sealed class Request
    {
        public int Id;
        public ulong Client;
        public int RewardType;
        public float CreatedAt;
        public bool Forced;
    }

    private readonly Dictionary<ulong, float> cooldownUntil = new Dictionary<ulong, float>();
    private Request open;
    private int nextId = 1;
    private bool forcedThisChain;
    private ulong? lastInterstitialTarget;

    public bool HasOpenRequest(float now)
    {
        ExpireOpen(now);
        return open != null;
    }

    public bool CanAsk(ulong client, float now) =>
        !cooldownUntil.TryGetValue(client, out float until) || now >= until;

    public bool TryOpen(ulong client, int rewardType, float now, out int requestId)
    {
        requestId = 0;
        ExpireOpen(now);
        if (open != null || !CanAsk(client, now))
            return false;
        open = new Request { Id = nextId++, Client = client, RewardType = rewardType, CreatedAt = now };
        requestId = open.Id;
        return true;
    }

    public AdResult Report(int requestId, ulong sender, AdOutcome outcome, float now)
    {
        if (open == null || open.Id != requestId || open.Client != sender)
            return AdResult.Rejected;
        if (now - open.CreatedAt > RequestExpirySeconds)
        {
            open = null;
            return AdResult.Rejected;
        }

        Request request = open;
        open = null;
        if (outcome == AdOutcome.Completed)
        {
            EndChain();
            return AdResult.Grant;
        }
        if (request.Forced)
        {
            // A forced ad that didn't finish is never forced again for this chain.
            return AdResult.Ended;
        }
        if (outcome == AdOutcome.Declined || outcome == AdOutcome.NoAd || outcome == AdOutcome.Timeout)
            cooldownUntil[request.Client] = now + AskCooldownSeconds;
        return AdResult.Reopen;
    }

    // Every client refused or is cooling down, and someone has an ad: the host may force one.
    public bool CanForce(IReadOnlyCollection<ulong> clients, Func<ulong, bool> hasAd, float now, bool forceEnabled)
    {
        if (!forceEnabled || forcedThisChain || HasOpenRequest(now) || clients == null || clients.Count == 0)
            return false;
        bool anyAd = false;
        foreach (ulong client in clients)
        {
            if (CanAsk(client, now)) return false;
            if (hasAd(client)) anyAd = true;
        }
        return anyAd;
    }

    public bool TryForce(IReadOnlyCollection<ulong> clients, Func<ulong, bool> hasAd, int rewardType, Random rng, float now,
        out ulong client, out int requestId)
    {
        client = 0;
        requestId = 0;
        if (!CanForce(clients, hasAd, now, true))
            return false;
        var withAd = new List<ulong>();
        foreach (ulong c in clients)
        {
            if (hasAd(c)) withAd.Add(c);
        }
        client = withAd[rng.Next(withAd.Count)];
        open = new Request { Id = nextId++, Client = client, RewardType = rewardType, CreatedAt = now, Forced = true };
        requestId = open.Id;
        forcedThisChain = true;
        return true;
    }

    // The host finished this ad attempt (reward granted, cancelled, or watched it itself).
    public void EndChain() => forcedThisChain = false;

    // The host is leaving: nothing open survives.
    public void CancelAll()
    {
        open = null;
        forcedThisChain = false;
        cooldownUntil.Clear();
        lastInterstitialTarget = null;
    }

    // The client that watches the host's next interstitial: rotates through the clients, starting
    // with the host's last rewarded pick, skipping phones that showed one less than 90 s ago.
    public ulong? NextInterstitialTarget(IReadOnlyList<ulong> clients, ulong? hostsPick,
        Func<ulong, float> lastInterstitialAt, float now)
    {
        if (clients == null || clients.Count == 0)
            return null;
        int start = 0;
        if (lastInterstitialTarget.HasValue)
        {
            int previous = IndexOf(clients, lastInterstitialTarget.Value);
            start = previous >= 0 ? previous + 1 : 0;
        }
        else if (hostsPick.HasValue)
        {
            int pick = IndexOf(clients, hostsPick.Value);
            start = pick >= 0 ? pick : 0;
        }
        for (int i = 0; i < clients.Count; i++)
        {
            ulong candidate = clients[(start + i) % clients.Count];
            if (now - lastInterstitialAt(candidate) >= InterstitialGapSeconds)
            {
                lastInterstitialTarget = candidate;
                return candidate;
            }
        }
        return null;
    }

    private void ExpireOpen(float now)
    {
        if (open != null && now - open.CreatedAt > RequestExpirySeconds)
            open = null;
    }

    private static int IndexOf(IReadOnlyList<ulong> list, ulong value)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == value) return i;
        }
        return -1;
    }
}
