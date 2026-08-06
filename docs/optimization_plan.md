# DailyCooking Optimization Plan

This document outlines the identified performance issues, architectural bottlenecks, and garbage collection (GC) hotspots within the **DailyCooking** codebase, along with concrete recommendations for future improvement.

---

## 🚨 Critical (GC Pressure Every Frame & Structural Bugs)

| Issue | Location | Impact & Recommended Fix |
|---|---|---|
| **`Enum.Equals()` Boxing** | `StateManager.cs:27` | `nextStateKey.Equals(_currentState.StateKey)` boxes both enum values as `object` every single frame on the hot update path.<br>**Fix:** Replace with `EqualityComparer<T>.Default.Equals(nextStateKey, _currentState.StateKey)` to allow the JIT compiler to optimize it to a direct integer comparison. |
| **Allocating `Physics.OverlapCapsule`** | `PlayerStateMachine.cs:222` | `HandleInteractions()` runs every `Update()` and allocates a new `Collider[]` array on every invocation.<br>**Fix:** Switch to `Physics.OverlapCapsuleNonAlloc()` with a pre-allocated static buffer (e.g., `private static readonly Collider[] hitBuffer = new Collider[10];`). |
| **`async void Update()` Allocations** | `InGameState.cs:30` | `public override async void Update()` allocates an async state machine struct every frame. If `IsShowingPopup()` is true, `await Task.Yield()` creates additional heap-allocated continuations back to the main thread.<br>**Fix:** Replace the async loop with a standard Unity `Coroutine` (`IEnumerator` + `yield return null`) which reuses the enumerator state machine and avoids heap allocations. |
| **Bot Update Guard Bug** | `BotCustomerController.cs:159`, `BotStateMachine.cs:37` | `if (!IsHost || !IsServer) return;` is used as a guard. On a dedicated server (`IsHost = false`, `IsServer = true`), this condition evaluates to `true`, completely preventing the bot state machines from updating.<br>**Fix:** Change the condition to `if (!IsHost && !IsServer) return;`. |
| **Broken `StopCoroutine` Usage** | `BotManager.cs:105` | `StopCoroutine(SpawnBotRoutine())` passes a **new** iterator, which does not match the active coroutine. The running coroutine is never stopped.<br>**Fix:** Store the returned `Coroutine` reference from `StartCoroutine()` in a private field, and pass that reference to `StopCoroutine()`. |

---

## ⚡ High (Network, GC, and Heavy CPU Hotspots)

### 1. Camera & Lookup Caching
*   **Cache `Camera.main`:** This property is referenced **24 times** across the codebase (e.g., in click handlers, movement logic, and UI positioning). `Camera.main` internally performs a costly `FindGameObjectsWithTag("MainCamera")` call every time. 
    *   **Fix:** Cache the main camera reference in `Awake()` or `Start()` into a private or static field.
*   **Deep Singleton Property Chains:** Expressions like `GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId)` are evaluated every frame inside `PlayerStateMachine.cs:266`.
    *   **Fix:** Cache the resulting `PlayerStats` reference in `Start()` or whenever the player session is initialized.

### 2. Recipe & Ingredient Lookups (O(n) Searches)
*   **Linear Recipe Scans:** Classes like `CuttingCounterController`, `PanCookingTool`, and `PotCookingTool` use linear `foreach` loops to find matching cutting, frying, and burning recipes.
    *   **Fix:** Pre-build lookups into a `Dictionary<KitchenObjectSO, TRecipe>` inside `Awake()` or `OnEnable()`.
*   **Garbage-Generating `ToList().IndexOf()`:** Inside `CuttingCounterController.cs:50-51, 118`, the code calls `_cuttingRecipeSOArray.ToList().IndexOf(...)` on every interaction and cut. Converting the array to a `List` allocates a new list on the heap each time.
    *   **Fix:** Use `Array.IndexOf(_cuttingRecipeSOArray, ...)` or a `Dictionary` lookup to completely eliminate heap allocations.
*   **Redundant Double Lookups:** `Cut()` calls `HasRecipeWithInput()` (which scans the array) and then calls `GetCuttingRecipeSOWithInput()` again immediately afterward if valid.
    *   **Fix:** Query the recipe once, cache it to a local variable, and perform null checks against the local cache.

### 3. Multiplayer & RPC Optimization
*   **Redundant ServerRpc-to-ClientRpc Chaining:** Many classes (e.g., `ContainerCounterController.cs:154-168`, `Table.cs`, `KitchenObject.cs`) use a pattern where a `ServerRpc` does nothing on the server and immediately calls a `ClientRpc` to notify all clients.
    *   **Fix:** Since these variables are synchronized via `NetworkVariable<T>`, subscribing to the `OnValueChanged` event on the client side handles this automatically. The RPC chain is redundant and doubles network traffic.
*   **GUID String Serialization:** Network RPCs frequently transmit 36-character string GUIDs (e.g., `kitchenObjectSO.Guid`). Strings are expensive to serialize due to character encoding and heap allocations upon receipt.
    *   **Fix:** Map GUIDs to integer indices or use `NetworkGuid` (from Unity Netcode) to reduce bandwidth and eliminate allocations.
*   **JSON Block Serialization:** `MultiplayerManager.cs:84-86` converts the entire game state to a single JSON string to send over RPC to joining clients. This is computationally expensive, generates garbage, and is prone to exceeding the packet limits of Unity Transport.
    *   **Fix:** Use structured `NetworkVariable` synchronization or binary stream writing instead of JSON.
*   **Ghost NetworkObject Churn:** `BuildingGhost.cs:127-153` destroys and re-spawns a networked object via the server every time the player shifts their selection or drag position.
    *   **Fix:** Use a local, non-networked pooled visual object for the ghost, and only spawn the networked object once the player commits to placement.

### 4. UI Garbage Generation
*   **Missing UI Object Pooling:** UI elements like tabs, inventory items, and popup frames in popups (`UICharacterPopup.cs`, `UIUpgradePopup.cs`, `UIMenuPopup.cs`) are repeatedly instantiated and destroyed whenever popups are opened or closed.
    *   **Fix:** Implement simple object pooling for frequently reused UI components to eliminate instantiation and deallocation churn.
*   **Excessive LINQ in UI Loops:** Opening the character popup or switching tabs triggers heavy LINQ chains (`Select()`, `Where()`, `FirstOrDefault()`, `ToList()`) on large lists (e.g., `CosmeticDatas`).
    *   **Fix:** Pre-cache cosmetic groups into dictionaries during initialization rather than re-filtering on the fly.

---

## 📈 Medium & Low Priority Improvements

### Medium
*   **Double Dictionary Searches:** `UIPopupManager.cs:42-52` calls `ContainsKey` and then immediately calls `TryGetValue` (or `GetPopup` scans a list). Use a single `TryGetValue` call.
*   **Enum `ToString()` Allocations:** Calling `.ToString()` on enums (e.g., `animationState.ToString()` in `BotCustomerController.cs:154`, `popupType.ToString()` in `UIPopupManager.cs:67`) allocates a boxed object and a string. Use a pre-computed dictionary or cached integer hashes (`Animator.StringToHash`).
*   **Material Leakage:** Accessing `Renderer.materials` inside `BotCustomerController.cs:350-353` generates a copy of the materials array every time. Use `Renderer.sharedMaterials` or cache the material references.
*   **List copying for iteration:** `activeBots.ToList()` in `BotManager.cs:82` creates a copy of the list. Iterate backward using a normal `for` loop to safely modify lists during iteration without allocating memory.
*   **Double Cast Type-Check:** `KitchenObject.cs:82-84` uses `is` and `as` sequentially:
    ```csharp
    if (this is TablewareKitchenObject)
        return this as TablewareKitchenObject;
    ```
    Replace with a single `as` cast and a null check:
    ```csharp
    var tableware = this as TablewareKitchenObject;
    if (tableware != null) return tableware;
    ```

### Low & Dead Code
*   **Empty Unity Lifecycle Methods:** `KitchenGameManager.cs:96` defines `private void Update() { }` which runs every frame. Unity invokes empty lifecycle methods regardless, incurring native-to-managed virtual call overhead. Remove all empty `Update()`, `Start()`, and `FixedUpdate()` methods.
*   **Component Destruction Bug:** `Table.cs:204` calls `Destroy(this)` instead of `Destroy(gameObject)`. This destroys only the component script, leaving the orphaned GameObject in the scene hierarchy.
*   **Wrong Operator in Null Check:** `UICharacterPopup.cs:150` uses `if (unfittables != null || unfittables.Count > 0)`. If `unfittables` is null, it checks `unfittables.Count` and throws a `NullReferenceException`. Change `||` to `&&`.

---

## 🛠️ Step-by-Step Action Plan for Implementation

When addressing these optimization points, follow this sequence:

1.  **Establish a Baseline:** Run the game with the **Unity Profiler** (using deep profiling if necessary) to observe the Garbage Collection (GC) spikes and CPU spikes during high-load gameplay (e.g., spawning many bots, placing objects on the grid).
2.  **Fix state-machine and loop allocations first (P0):** Correct the boxing in `StateManager.cs` and the capsule overlap allocations in `PlayerStateMachine.cs`. These provide the most noticeable reductions in micro-stutter.
3.  **Optimize Recipe Lookups:** Implement `Dictionary` caches for cutting, frying, and cooking recipes to eliminate O(n) lookups.
4.  **Introduce Pooling:** Set up object pools for UI elements and the grid placement ghost.
5.  **Re-profile:** Run the Unity Profiler again to confirm that frame-rate stability has improved and the GC frequency has dropped significantly.
