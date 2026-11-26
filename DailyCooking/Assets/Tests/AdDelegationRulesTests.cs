using System;
using System.Collections.Generic;
using NUnit.Framework;

public class AdDelegationRulesTests
{
    private const int Coin = 4;
    private static readonly List<ulong> Clients = new List<ulong> { 1, 2 };
    private static bool AllHaveAds(ulong c) => true;
    private static bool NoneHaveAds(ulong c) => false;

    private static int Open(AdDelegationRules r, ulong client, float now)
    {
        Assert.IsTrue(r.TryOpen(client, Coin, now, out int id));
        return id;
    }

    [Test] public void Open_OneAtATime()
    {
        var r = new AdDelegationRules();
        Open(r, 1, 0f);
        Assert.IsFalse(r.TryOpen(2, Coin, 1f, out _));
    }

    [Test] public void Completed_Grants_Once()
    {
        var r = new AdDelegationRules();
        int id = Open(r, 1, 0f);
        Assert.AreEqual(AdDelegationRules.AdResult.Grant, r.Report(id, 1, AdOutcome.Completed, 10f));
        Assert.AreEqual(AdDelegationRules.AdResult.Rejected, r.Report(id, 1, AdOutcome.Completed, 11f));
    }

    [Test] public void WrongSender_Rejected()
    {
        var r = new AdDelegationRules();
        int id = Open(r, 1, 0f);
        Assert.AreEqual(AdDelegationRules.AdResult.Rejected, r.Report(id, 2, AdOutcome.Completed, 5f));
        Assert.AreEqual(AdDelegationRules.AdResult.Grant, r.Report(id, 1, AdOutcome.Completed, 6f));
    }

    [Test] public void Expired_Rejected()
    {
        var r = new AdDelegationRules();
        int id = Open(r, 1, 0f);
        Assert.AreEqual(AdDelegationRules.AdResult.Rejected, r.Report(id, 1, AdOutcome.Completed, 121f));
        Assert.IsTrue(r.TryOpen(2, Coin, 122f, out _), "an expired request no longer blocks a new one");
    }

    [Test] public void Declined_Cooldown60s_ThenAskable()
    {
        var r = new AdDelegationRules();
        int id = Open(r, 1, 0f);
        Assert.AreEqual(AdDelegationRules.AdResult.Reopen, r.Report(id, 1, AdOutcome.Declined, 5f));
        Assert.IsFalse(r.CanAsk(1, 30f));
        Assert.IsFalse(r.TryOpen(1, Coin, 30f, out _));
        r.EndChain();
        Assert.IsFalse(r.CanAsk(1, 64f), "cooldown outlives the request");
        Assert.IsTrue(r.CanAsk(1, 65.1f));
    }

    [Test] public void NoAd_Cooldown_Reopen()
    {
        var r = new AdDelegationRules();
        int id = Open(r, 1, 0f);
        Assert.AreEqual(AdDelegationRules.AdResult.Reopen, r.Report(id, 1, AdOutcome.NoAd, 1f));
        Assert.IsFalse(r.CanAsk(1, 2f));
        Assert.IsTrue(r.CanAsk(2, 2f));
    }

    [Test] public void Timeout_Cooldown_Reopen()
    {
        var r = new AdDelegationRules();
        int id = Open(r, 1, 0f);
        Assert.AreEqual(AdDelegationRules.AdResult.Reopen, r.Report(id, 1, AdOutcome.Timeout, 15f));
        Assert.IsFalse(r.CanAsk(1, 16f));
    }

    [Test] public void Failed_Reopen_NoCooldown()
    {
        var r = new AdDelegationRules();
        int id = Open(r, 1, 0f);
        Assert.AreEqual(AdDelegationRules.AdResult.Reopen, r.Report(id, 1, AdOutcome.FailedOrClosedEarly, 20f));
        Assert.IsTrue(r.CanAsk(1, 21f));
    }

    [Test] public void AllInCooldown_CanForce()
    {
        var r = new AdDelegationRules();
        r.Report(Open(r, 1, 0f), 1, AdOutcome.Declined, 1f);
        Assert.IsFalse(r.CanForce(Clients, AllHaveAds, 2f, true), "client 2 can still be asked");
        r.Report(Open(r, 2, 2f), 2, AdOutcome.Declined, 3f);
        Assert.IsTrue(r.CanForce(Clients, AllHaveAds, 4f, true));
    }

    [Test] public void CanForce_FalseWhenNoAdAnywhere()
    {
        var r = new AdDelegationRules();
        r.Report(Open(r, 1, 0f), 1, AdOutcome.NoAd, 1f);
        r.Report(Open(r, 2, 1f), 2, AdOutcome.NoAd, 2f);
        Assert.IsFalse(r.CanForce(Clients, NoneHaveAds, 3f, true));
    }

    [Test] public void CanForce_FalseWhenSwitchOff()
    {
        var r = new AdDelegationRules();
        r.Report(Open(r, 1, 0f), 1, AdOutcome.Declined, 1f);
        r.Report(Open(r, 2, 1f), 2, AdOutcome.Declined, 2f);
        Assert.IsFalse(r.CanForce(Clients, AllHaveAds, 3f, false));
    }

    [Test] public void Force_PicksOnlyClientsWithAd()
    {
        var r = new AdDelegationRules();
        r.Report(Open(r, 1, 0f), 1, AdOutcome.Declined, 1f);
        r.Report(Open(r, 2, 1f), 2, AdOutcome.Declined, 2f);
        for (int seed = 0; seed < 20; seed++)
        {
            var copy = new AdDelegationRules();
            copy.Report(Open(copy, 1, 0f), 1, AdOutcome.Declined, 1f);
            copy.Report(Open(copy, 2, 1f), 2, AdOutcome.Declined, 2f);
            Assert.IsTrue(copy.TryForce(Clients, c => c == 2, Coin, new Random(seed), 3f, out ulong picked, out _));
            Assert.AreEqual(2UL, picked);
        }
    }

    [Test] public void Forced_Failed_Ends_NoSecondForce()
    {
        var r = new AdDelegationRules();
        r.Report(Open(r, 1, 0f), 1, AdOutcome.Declined, 1f);
        r.Report(Open(r, 2, 1f), 2, AdOutcome.Declined, 2f);
        Assert.IsTrue(r.TryForce(Clients, AllHaveAds, Coin, new Random(1), 3f, out ulong picked, out int id));
        Assert.AreEqual(AdDelegationRules.AdResult.Ended, r.Report(id, picked, AdOutcome.FailedOrClosedEarly, 10f));
        Assert.IsFalse(r.CanForce(Clients, AllHaveAds, 11f, true));
    }

    [Test] public void Forced_Completed_Grants()
    {
        var r = new AdDelegationRules();
        r.Report(Open(r, 1, 0f), 1, AdOutcome.Declined, 1f);
        r.Report(Open(r, 2, 1f), 2, AdOutcome.Declined, 2f);
        r.TryForce(Clients, AllHaveAds, Coin, new Random(1), 3f, out ulong picked, out int id);
        Assert.AreEqual(AdDelegationRules.AdResult.Grant, r.Report(id, picked, AdOutcome.Completed, 30f));
    }

    [Test] public void HostLeaves_CancelsOpenRequest()
    {
        var r = new AdDelegationRules();
        int id = Open(r, 1, 0f);
        r.CancelAll();
        Assert.AreEqual(AdDelegationRules.AdResult.Rejected, r.Report(id, 1, AdOutcome.Completed, 5f));
        Assert.IsTrue(r.TryOpen(2, Coin, 6f, out _));
    }

    [Test] public void Interstitial_StartsWithHostsPick()
    {
        var r = new AdDelegationRules();
        var clients = new List<ulong> { 1, 2, 3 };
        Assert.AreEqual(2UL, r.NextInterstitialTarget(clients, 2, c => float.NegativeInfinity, 0f));
    }

    [Test] public void Interstitial_RotatesAfterPrevious()
    {
        var r = new AdDelegationRules();
        var clients = new List<ulong> { 1, 2, 3 };
        Func<ulong, float> never = c => float.NegativeInfinity;
        Assert.AreEqual(2UL, r.NextInterstitialTarget(clients, 2, never, 0f));
        Assert.AreEqual(3UL, r.NextInterstitialTarget(clients, 2, never, 300f));
        Assert.AreEqual(1UL, r.NextInterstitialTarget(clients, 2, never, 600f));
    }

    [Test] public void Interstitial_SkipsUnder90s()
    {
        var r = new AdDelegationRules();
        var clients = new List<ulong> { 1, 2 };
        var last = new Dictionary<ulong, float> { { 1, 950f }, { 2, 0f } };
        Assert.AreEqual(2UL, r.NextInterstitialTarget(clients, 1, c => last[c], 1000f));
    }

    [Test] public void Interstitial_NoneEligible_Null()
    {
        var r = new AdDelegationRules();
        var clients = new List<ulong> { 1, 2 };
        Assert.IsNull(r.NextInterstitialTarget(clients, null, c => 990f, 1000f));
        Assert.IsNull(r.NextInterstitialTarget(new List<ulong>(), null, c => 0f, 1000f));
    }
}
