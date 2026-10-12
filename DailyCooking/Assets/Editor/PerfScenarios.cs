using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

// Editor-only drivers for the profiling scenarios in docs/plans/13-profiler-baseline.md.
// They replay the exact code path the MainMenu buttons run (UIMainMenuPopup.cs:20-39), so the
// work the profiler sees matches production instead of a shortcut.
// Never part of a build: lives under Assets/Editor/.
public static class PerfScenarios
{
    // Scratch save; deleted by "Tools/Profiling/Delete PerfTest save".
    const string SaveName = "PerfTest";
    const string SavePassword = "perf123";
    const int MenuOpenCycles = 10;

    [MenuItem("Tools/Profiling/Enter Game (new host)")]
    public static void EnterGame() { EnterGameAsync(); }

    [MenuItem("Tools/Profiling/S5 - Open Shop popup")]
    public static void S5_OpenShop()
    {
        RunInRestaurant(() =>
        {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIShopPopup);
            Debug.Log("[Perf] S5: shop popup opened. Cycle the tabs by hand, then stop the recorder.");
        });
    }

    [MenuItem("Tools/Profiling/S6 - Menu popup x10, then every category")]
    public static void S6_MenuPopup()
    {
        RunInRestaurant(() =>
        {
            var popups = UIPopupManager.Instance;
            for (int i = 0; i < MenuOpenCycles; i++)
                popups.ShowPopup(UIPopupType.UIMenuPopup);

            // One more open, then click every category tab to measure the per-tab rebuild.
            popups.ShowPopup(UIPopupType.UIMenuPopup);
            var categoryItems = UnityEngine.Object.FindObjectsByType<UIMenuCategoryItem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var categoryItem in categoryItems)
                categoryItem.CategoryButton.onClick.Invoke();

            Debug.Log($"[Perf] S6: {MenuOpenCycles} menu opens + {categoryItems.Length} category clicks.");
        });
    }

    [MenuItem("Tools/Profiling/S10 - ShakeCamera 10s")]
    public static void S10_Shake()
    {
        RunInRestaurant(() =>
        {
            CodeMonkey.Utils.UtilsClass.ShakeCamera(0.5f, 10f);
            Debug.Log("[Perf] S10: camera shake running for 10s.");
        });
    }

    [MenuItem("Tools/Profiling/Delete PerfTest save")]
    public static void DeletePerfSave()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[Perf] no GameManager yet.");
            return;
        }
        var existing = GameManager.Instance.SavedDataList.Find(s => s.GameDataName == SaveName);
        if (existing == null)
        {
            Debug.Log("[Perf] no PerfTest save to delete.");
            return;
        }
        GameManager.Instance.DeleteSavedData(existing);
        Debug.Log("[Perf] PerfTest save removed from the list.");
    }

    static async void EnterGameAsync()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[Perf] Enter Play Mode first.");
            return;
        }
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[Perf] already hosting — skip.");
            return;
        }

        // NewGame refuses to overwrite an existing save, so drop the scratch one first.
        var existing = GameManager.Instance.SavedDataList.Find(s => s.GameDataName == SaveName);
        if (existing != null)
            GameManager.Instance.DeleteSavedData(existing);

        if (!GameManager.Instance.NewGame(SaveName, SavePassword))
        {
            Debug.LogError("[Perf] NewGame failed.");
            return;
        }

        string joinCode = await MultiplayerManager.Instance.StartHostSessionAsync();
        if (string.IsNullOrEmpty(joinCode))
        {
            // Fallback: offline host on the single-player transport. NOT the production path —
            // treat numbers taken here as a rough lower bound, not as production figures.
            Debug.LogWarning("[Perf] Relay host failed (Auth/Relay unreachable). Falling back to StartSinglePlayer — NOT the production path.");
            MultiplayerManager.Instance.StartSinglePlayer();
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                Debug.LogError("[Perf] StartSinglePlayer failed too. Cannot enter the game.");
                return;
            }
        }
        else
        {
            Debug.Log($"[Perf] host up, join code {joinCode}");
        }

        Loader.LoadNetwork(Loader.Scene.GameScene);
        GameManager.Instance.SwitchState(new InGameState(GameManager.Instance));
        UIPopupManager.Instance.HidePopup(UIPopupType.UIMainMenuPopup);
    }

    // Everything below needs a live restaurant, otherwise the code paths under test early-return.
    static void RunInRestaurant(Action action)
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[Perf] Enter Play Mode first.");
            return;
        }
        if (GameManager.Instance == null || GameManager.Instance.GameData == null)
        {
            Debug.LogError("[Perf] not in a restaurant yet — run 'Tools/Profiling/Enter Game (new host)' first.");
            return;
        }
        action();
    }
}
