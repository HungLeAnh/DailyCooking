using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Picks and applies the graphics tier and frame rate at launch, and (Graphics = Auto) lowers them
// while playing when the phone can't hold 60 FPS. Auto runs in Android players, or in the Editor
// when forced; elsewhere Auto keeps the level the app started with. Manual levels always apply.
public class MobilePerformanceManager : MonoBehaviour
{
    private const string Log = "[Perf] ";
    private const float IgnoreAfterSceneLoadSeconds = 5f;
    private const int DefaultFps = 60;

    public static MobilePerformanceManager Instance { get; private set; }

    // Lets the Editor run Auto (device pick + governor) for testing.
    public static bool ForceAutoInEditor;

    public GraphicsPreferences Preferences { get; private set; } = GraphicsPreferences.Default;
    public MobileTier? CurrentTier { get; private set; }
    public int CurrentFps { get; private set; } = DefaultFps;
    public bool GovernorActive => governor != null;

    public event Action<MobileTier?> OnTierChanged;

    private FrameRateGovernor governor;
    private MobileTier devicePick;
    private int startLevel;
    private float ignoreUntil;
    private bool ranAutoThisSession;
    private bool downgradedThisSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (Instance != null)
            return;
        var go = new GameObject(nameof(MobilePerformanceManager));
        DontDestroyOnLoad(go);
        go.AddComponent<MobilePerformanceManager>();
    }

    private static bool AutoAllowed
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#elif UNITY_EDITOR
            return ForceAutoInEditor || PlayerPrefs.GetInt(PerfPrefs.EditorForceAutoKey, 0) != 0;
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        startLevel = QualitySettings.GetQualityLevel();
        devicePick = PickDevice();
        Preferences = PerfPrefs.LoadPreferences();

        ProcessLastSession();
        Apply("launch");
        SceneManager.sceneLoaded += OnSceneLoaded;
        ignoreUntil = Time.unscaledTime + IgnoreAfterSceneLoadSeconds;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

#if UNITY_EDITOR
    // Play Mode must not leave the Editor on a Mobile level.
    private void OnApplicationQuit()
    {
        if (QualitySettings.GetQualityLevel() != startLevel)
            QualitySettings.SetQualityLevel(startLevel, true);
    }
#endif

    public void SetPreferences(GraphicsPreferences p)
    {
        Preferences = GraphicsPreferencesRules.Sanitize((int)p.Mode, p.ChosenFps);
        PerfPrefs.SavePreferences(Preferences);
        Apply("settings");
    }

    private void Update() => FeedFrame(Time.unscaledDeltaTime);

    // One frame of the governor; Update feeds real frame times, tests may feed their own.
    public void FeedFrame(float unscaledDt)
    {
        if (governor == null || Time.unscaledTime < ignoreUntil)
            return;

        GovernorDecision decision = governor.Feed(unscaledDt);
        if (decision == GovernorDecision.None)
            return;

        downgradedThisSession = true;
        PerfPrefs.SaveSession(ranAutoThisSession, true);
        PerfPrefs.SaveMemory(PerfMemory.AfterDowngrade(PerfPrefs.LoadMemory(), governor.Tier, governor.TargetFps));
        Debug.Log(Log + (decision == GovernorDecision.DropTier
            ? $"governor: tier -> {MobileTiers.QualityLevelName(governor.Tier)}"
            : "governor: frame rate -> 30"));
        ApplyTier(governor.Tier);
        ApplyFps(governor.TargetFps);
        if (governor.Finished)
            Debug.Log(Log + "governor: nothing left to lower, stopped");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ignoreUntil = Time.unscaledTime + IgnoreAfterSceneLoadSeconds;
        governor?.ResetWindow();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            governor?.ResetWindow();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
            governor?.ResetWindow();
    }

    // Counts the previous Auto session toward the retry (or not, if it downgraded), then starts fresh.
    private void ProcessLastSession()
    {
        var (ranAuto, downgraded) = PerfPrefs.LoadLastSession();
        if (ranAuto)
            PerfPrefs.SaveMemory(PerfMemory.AfterSession(PerfPrefs.LoadMemory(), downgraded, devicePick));
        PerfPrefs.SaveSession(false, false);
    }

    private void Apply(string reason)
    {
        bool killSwitchOn = PerfPrefs.KillSwitchOn;
        bool autoAllowed = AutoAllowed;
        MobileTier? manual = GraphicsPreferencesRules.ManualTier(Preferences);
        PerfMemoryState memory = PerfPrefs.LoadMemory();
        governor = null;

        if (manual.HasValue)
        {
            ApplyTier(manual.Value);
            ApplyFps(GraphicsPreferencesRules.StartFps(Preferences, DefaultFps));
        }
        else if (autoAllowed)
        {
            var (tier, fps) = PerfMemory.StartupChoice(devicePick, memory);
            ApplyTier(tier);
            ApplyFps(GraphicsPreferencesRules.StartFps(Preferences, fps));
            if (GraphicsPreferencesRules.GovernorRuns(Preferences, killSwitchOn, autoAllowed))
            {
                governor = new FrameRateGovernor(tier, CurrentFps, GraphicsPreferencesRules.FpsLocked(Preferences));
                ranAutoThisSession = true;
                PerfPrefs.SaveSession(true, downgradedThisSession);
            }
        }
        else
        {
            ApplyLevel(startLevel);
            SetTier(null);
            ApplyFps(GraphicsPreferencesRules.StartFps(Preferences, DefaultFps));
        }

        Debug.Log(Log + $"{reason}: device RAM {SystemInfo.systemMemorySize} MB, GPU {SystemInfo.graphicsMemorySize} MB, " +
                  $"{SystemInfo.processorCount} cores -> pick {devicePick}; remembered tier {memory.RememberedTier}, " +
                  $"fps {memory.RememberedFps}, clean {memory.CleanSessions}; mode {Preferences.Mode}, " +
                  $"chosen fps {Preferences.ChosenFps}; applied {QualitySettings.names[QualitySettings.GetQualityLevel()]} " +
                  $"@ {CurrentFps} FPS; governor {(governor != null ? "on" : "off")} " +
                  $"(auto allowed {autoAllowed}, kill switch {(killSwitchOn ? "on" : "off")})");
    }

    private void ApplyTier(MobileTier tier)
    {
        string name = MobileTiers.QualityLevelName(tier);
        int index = Array.IndexOf(QualitySettings.names, name);
        if (index < 0)
        {
            Debug.LogWarning(Log + $"quality level '{name}' not found; level unchanged");
            return;
        }
        ApplyLevel(index);
        SetTier(tier);
    }

    private static void ApplyLevel(int index)
    {
        if (QualitySettings.GetQualityLevel() != index)
            QualitySettings.SetQualityLevel(index, true);
    }

    private void SetTier(MobileTier? tier)
    {
        if (CurrentTier == tier)
            return;
        CurrentTier = tier;
        OnTierChanged?.Invoke(tier);
    }

    private void ApplyFps(int fps)
    {
        CurrentFps = fps;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps;
    }

    private static MobileTier PickDevice()
    {
#if UNITY_EDITOR
        int fake = PlayerPrefs.GetInt(PerfPrefs.EditorDevicePickKey, -1);
        if (fake >= 0 && fake <= (int)MobileTier.High)
            return (MobileTier)fake;
#endif
        return DeviceTierPicker.Pick(SystemInfo.systemMemorySize, SystemInfo.graphicsMemorySize, SystemInfo.processorCount);
    }
}
