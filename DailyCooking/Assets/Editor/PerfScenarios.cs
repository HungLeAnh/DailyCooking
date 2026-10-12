using System;
using System.Collections.Generic;
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

    [MenuItem("Tools/Verify/Open Menu popup once")]
    public static void Verify_OpenMenu()
    {
        RunInRestaurant(() =>
        {
            // UIMenuPopup.ShowMenuOfType lists every ConfigFood.FoodItems entry regardless of
            // unlock state, so Stew's price/exp are readable here even on a fresh save.
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIMenuPopup);
            Debug.Log("[Perf] menu popup opened once.");
        });
    }

    [MenuItem("Tools/Verify/Open Shop popup once")]
    public static void Verify_OpenShop()
    {
        RunInRestaurant(() =>
        {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIShopPopup);
            Debug.Log("[Perf] shop popup opened once.");
        });
    }

    [MenuItem("Tools/Verify/Unlock all dishes + open Menu popup")]
    public static void Verify_UnlockAllDishes()
    {
        RunInRestaurant(() =>
        {
            var gameData = GameManager.Instance.GameData;
            gameData.RestaurantData.Level = 99;
            foreach (var food in ConfigManager.Instance.ConfigFood.FoodItems)
                gameData.MenuData.AddUnlockedDish(food);

            // UIFoodItem.SetMenuFoodItem writes foodPriceText unconditionally, but a locked item
            // covers it with lockedOverlayImage and swaps foodStatusText for the lock message —
            // so "$35" only becomes visible once the dish is unlocked.
            var stew = ConfigManager.Instance.ConfigFood.FoodItems.Find(x => x.recipeName == "Stew");
            if (stew != null)
                Debug.Log($"[Perf] stew price=${stew.price} exp={stew.exp} unlockLevel={stew.unlockLevel}");

            UIPopupManager.Instance.HidePopup(UIPopupType.UIMenuPopup);
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIMenuPopup);
        });
    }

    [MenuItem("Tools/Verify/Cooked vs burned stew in option menu")]
    public static void Verify_BurnedStewName()
    {
        RunInRestaurant(() =>
        {
            // UIOptionMenuPopup.ShowPopup(null) calls HidePopup(): it needs a Param carrying a
            // live IHasOptionalSO sender, otherwise BaseCounter_OnShowOptionalMenu returns before
            // it instantiates any item, so a bare ShowPopup shows nothing.
            var cooked = AssetDatabase.LoadAssetAtPath<KitchenObjectSO>("Assets/SO/KitchenObjectSO/StewCookedSO.asset");
            var burned = AssetDatabase.LoadAssetAtPath<KitchenObjectSO>("Assets/SO/KitchenObjectSO/StewBurnedSO.asset");

            IHasOptionalSO sender = UnityEngine.Object.FindFirstObjectByType<OptionalContainerCounterController>();
            if (sender == null) sender = UnityEngine.Object.FindFirstObjectByType<CookingToolCounterController>();
            if (sender == null) sender = UnityEngine.Object.FindFirstObjectByType<PostBox>();

            if (sender == null || cooked == null || burned == null)
            {
                Debug.LogError($"[Perf] verify 08 unavailable: sender={sender} cooked={cooked} burned={burned}");
                return;
            }

            UIPopupManager.Instance.HidePopup(UIPopupType.UIMenuPopup);
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIOptionMenuPopup, new UIOptionMenuPopup.Param
            {
                sender = sender,
                optionalList = new List<KitchenObjectSO> { cooked, burned },
                Title = "Pick output"
            });
            Debug.Log($"[Perf] cooked='{cooked.objectName}' burned='{burned.objectName}'");
        });
    }

    [MenuItem("Tools/Verify/Hide tutorial + show Stew tile")]
    public static void Verify_HideTutorialShowStew()
    {
        RunInRestaurant(() =>
        {
            // The tutorial draws Window_QuestPointer's focus window straight over the menu popup,
            // which hides exactly the tile being verified and also swallows screen clicks.
            int hidden = 0;
            foreach (var panel in UnityEngine.Object.FindObjectsByType<TutorialPanel>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                panel.gameObject.SetActive(false);
                hidden++;
            }
            foreach (var pointer in UnityEngine.Object.FindObjectsByType<Window_QuestPointer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                pointer.gameObject.SetActive(false);
                hidden++;
            }

            UIPopupManager.Instance.HidePopup(UIPopupType.UIMenuPopup);
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIMenuPopup);
            Debug.Log($"[Perf] hid {hidden} tutorial objects; menu popup reopened.");
        });
    }

    // S4 needs a selected object before BuildingGhost.OnPanStarted will raycast: it early-returns
    // when placedObjectTypeSO is null, which is why an editor script alone cannot drive the drag.
    // Selecting here and then dragging with a real mouse gives HandleDraging its Camera.main calls.
    [MenuItem("Tools/Profiling/S4 - Start placement (ghost)")]
    public static void S4_StartPlacement()
    {
        RunInRestaurant(() =>
        {
            var database = GridBuildingSystem.Instance.PlacedObjectDatabase.PlacedObjects;
            if (database == null || database.Count == 0)
            {
                Debug.LogError("[Perf] S4: PlacedObjectDatabase is empty.");
                return;
            }

            // Park the ghost under the middle of the game view so the mouse can reach it.
            var cam = Camera.main;
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.45f));
            if (!Physics.Raycast(ray, out RaycastHit hit, 999f))
            {
                Debug.LogError("[Perf] S4: nothing hit under the screen centre.");
                return;
            }

            var so = database[0];
            GridBuildingSystem.Instance.BuildingPlacementManager.SetPlacedObjectTypeSO(so, hit.point);
            Debug.Log($"[Perf] S4: placement started with '{so.name}' at {hit.point}.");
        });
    }

    // Read back what S4 actually needs: BuildingGhost early-returns in HandleDraging unless
    // isDragging is true, and OnPanStarted refuses to set it while the cursor is over UI.
    // Call it while the mouse is held on the ghost to see which guard stopped the drag.
    [MenuItem("Tools/Verify/S4 state (ghost / drag / mouse)")]
    public static void S4State()
    {
        RunInRestaurant(() =>
        {
            var ghost = BuildingGhost.Instance;
            var selected = GridBuildingSystem.Instance.BuildingPlacementManager.PlacedObjectTypeSO;
            var ptr = UnityEngine.InputSystem.Pointer.current;
            var cam = Camera.main;

            string ghostWhere = "no-ghost";
            if (ghost != null)
            {
                Vector3 world = ghost.transform.position;
                Vector3 screen = cam.WorldToScreenPoint(world);
                bool onScreen = screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width && screen.y >= 0f && screen.y <= Screen.height;
                ghostWhere = $"world={world} screen={screen} onScreen={onScreen}";
            }

            Debug.Log(
                $"[Perf] S4state isDragging={(ghost != null ? ghost.IsDragging.ToString() : "no-ghost")} " +
                $"selected={(selected != null ? selected.name : "null")} " +
                $"stopMoving={(ghost != null ? ghost.StopMoving.ToString() : "-")} " +
                $"mouseOverUI={GameInput.Instance.IsMouseOverUI()} " +
                $"pointerPos={(ptr != null ? ptr.position.ReadValue().ToString() : "null")} " +
                $"ghost {ghostWhere}");
        });
    }

    [MenuItem("Tools/Profiling/S4 - Cancel placement")]
    public static void S4_CancelPlacement()
    {
        RunInRestaurant(() =>
        {
            GridBuildingSystem.Instance.BuildingPlacementManager.SetPlacedObjectTypeSO(null, Vector3.zero);
            Debug.Log("[Perf] S4: placement cancelled.");
        });
    }

    // Hold W (or any key / the mouse) while this runs: it reports whether the Input System
    // actually sees the input, whether the movement action reads it, and whether the owner has
    // stats — HandleMovement returns early on any of those three.
    [MenuItem("Tools/Verify/Input diagnostics (hold input while it runs)")]
    public static void InputDiagnostics()
    {
        RunInRestaurant(async () =>
        {
            for (int i = 0; i < 8; i++)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                var ptr = UnityEngine.InputSystem.Pointer.current;
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerStateMachine>();
                var stats = player != null ? player.GetOwnerStats() : null;
                Debug.Log(
                    $"[Perf] input #{i} " +
                    $"w={(kb != null ? kb.wKey.isPressed.ToString() : "no-kb")} " +
                    $"pointerPos={(ptr != null ? ptr.position.ReadValue().ToString() : "no-pointer")} " +
                    $"press={(ptr != null ? ptr.press.isPressed.ToString() : "-")} " +
                    $"move={GameInput.Instance.GetMovementVectorNormalized()} " +
                    $"playerPos={(player != null ? player.transform.position.ToString() : "none")} " +
                    $"ownerStats={(stats != null ? $"ok moveSpeed={stats.MoveSpeed}" : "NULL")}");
                await Task.Delay(350);
            }
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
