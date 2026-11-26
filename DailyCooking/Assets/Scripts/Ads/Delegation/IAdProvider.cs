using System;
using UnityEngine;

// One phone's ads, reported as an outcome so the ad hand-over can tell "watched" from "failed".
public interface IAdProvider
{
    bool HasRewarded { get; }
    bool HasInterstitial { get; }
    bool IsShowing { get; }
    void ShowRewarded(string placement, Action<AdOutcome> done);
    void ShowInterstitial(string placement, Action<AdOutcome> done);
}

public static class AdProviders
{
    private static IAdProvider current;

    // The Editor has no ad units: it uses the fake (3 s, then Completed) so flows can be tested.
    public static IAdProvider Current
    {
        get
        {
            if (current != null) return current;
#if UNITY_EDITOR
            Fake = new FakeAdProvider();
            FakeAdTicker.Ensure(Fake);
            current = Fake;
#else
            current = new LevelPlayAdProvider();
#endif
            return current;
        }
    }

    // Editor test hook: set HasAd / NextOutcome / DelaySeconds from execute_code.
    public static FakeAdProvider Fake { get; private set; }
}

// Drives FakeAdProvider in Play Mode.
public class FakeAdTicker : MonoBehaviour
{
    private FakeAdProvider fake;

    public static void Ensure(FakeAdProvider fake)
    {
        if (!Application.isPlaying) return;
        var go = new GameObject(nameof(FakeAdTicker));
        DontDestroyOnLoad(go);
        go.AddComponent<FakeAdTicker>().fake = fake;
    }

    private void Update() => fake?.Tick(Time.unscaledDeltaTime);
}
