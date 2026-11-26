using UnityEngine;

// PlayerPrefs storage for the mobile performance settings (device-local).
public static class PerfPrefs
{
    public const string GovernorEnabledKey = "perf.governor.enabled";
    public const string ModeKey = "perf.graphics.mode";
    public const string ChosenFpsKey = "perf.graphics.fps";
    public const string MemoryTierKey = "perf.memory.tier";
    public const string MemoryFpsKey = "perf.memory.fps";
    public const string MemoryCleanKey = "perf.memory.clean";
    public const string SessionAutoKey = "perf.session.auto";
    public const string SessionDowngradedKey = "perf.session.downgraded";

    // Editor-only test hooks (read only under UNITY_EDITOR): force Auto, and fake the device pick (0-2).
    public const string EditorForceAutoKey = "perf.editor.forceAuto";
    public const string EditorDevicePickKey = "perf.editor.devicePick";

    public static bool KillSwitchOn => PlayerPrefs.GetInt(GovernorEnabledKey, 1) != 0;

    public static GraphicsPreferences LoadPreferences() =>
        GraphicsPreferencesRules.Sanitize(PlayerPrefs.GetInt(ModeKey, 0), PlayerPrefs.GetInt(ChosenFpsKey, 0));

    public static void SavePreferences(GraphicsPreferences p)
    {
        PlayerPrefs.SetInt(ModeKey, (int)p.Mode);
        PlayerPrefs.SetInt(ChosenFpsKey, p.ChosenFps);
        PlayerPrefs.Save();
    }

    public static PerfMemoryState LoadMemory() => new PerfMemoryState
    {
        RememberedTier = PlayerPrefs.GetInt(MemoryTierKey, -1),
        RememberedFps = PlayerPrefs.GetInt(MemoryFpsKey, 0),
        CleanSessions = Mathf.Max(0, PlayerPrefs.GetInt(MemoryCleanKey, 0)),
    };

    public static void SaveMemory(PerfMemoryState s)
    {
        PlayerPrefs.SetInt(MemoryTierKey, s.RememberedTier);
        PlayerPrefs.SetInt(MemoryFpsKey, s.RememberedFps);
        PlayerPrefs.SetInt(MemoryCleanKey, s.CleanSessions);
        PlayerPrefs.Save();
    }

    // Whether the previous session ran Auto with the governor, and whether it had to downgrade.
    public static (bool ranAuto, bool downgraded) LoadLastSession() =>
        (PlayerPrefs.GetInt(SessionAutoKey, 0) != 0, PlayerPrefs.GetInt(SessionDowngradedKey, 0) != 0);

    public static void SaveSession(bool ranAuto, bool downgraded)
    {
        PlayerPrefs.SetInt(SessionAutoKey, ranAuto ? 1 : 0);
        PlayerPrefs.SetInt(SessionDowngradedKey, downgraded ? 1 : 0);
        PlayerPrefs.Save();
    }
}
