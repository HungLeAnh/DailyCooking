# DailyCooking — Data Flow and Network Authority

How game state moves between the host and clients. The rule everywhere: **the host (server)
decides, clients ask.** Clients send intents over RPCs; the server validates them against its
own state and config, applies the change, and the result reaches clients through
NetworkVariables/NetworkLists, spawns, or `SendTo.NotServer` mirror RPCs.

## 1. Content (edit time)

- All ScriptableObjects derive from `SerializableScriptableObject`; `OnValidate` stamps a GUID
  from the asset path. Saves and the network protocol reference content only by these GUIDs.
- `RecipeDatabaseSO` is the single source of recipes (dictionary caches built in `Initialize`).
- Prices and rewards live in config: `ConfigShop`, `ConfigFood`, `ConfigUpgrade`,
  `Customization_Data` (on `ConfigManager`), and `ShopManager` (daily free rewards, gem→coin
  exchange offers). The server always reads prices from here, never from the client.
- `ShopItemType` is stored as numbers in scenes/prefabs/assets: only append new values.

## 2. Session and join

1. Host: `GameManager.NewGame/LoadGame` → `MultiplayerManager.StartHostSessionAsync` → Relay
   allocation (`SessionManager.StartHostWithRelay`) → `Loader.LoadNetwork(GameScene)`.
2. Client: `MultiplayerManager.StartClientSession(joinCode)` → Relay join. The client drops any
   local save (`GameManager.ClearActiveSave`) and follows the host's scene.
3. On connect the client sends its name and player id (`SetplayerName/IdServerRpc`, keyed by the
   RPC sender), then asks for the game data (`SyncDataToNewClientServerRpc`).
4. The host serializes its `GameData`, gzips it and sends it in 4 KB chunks
   (`ReceiveGameDataChunkClientRpc`) — a full restaurant does not fit in one message
   (UnityTransport Max Payload Size is 6144). The client reassembles it and calls
   `GameManager.ReplaceGameDataFromHost`, then `OnDataSyncToNewClient` lets the grid and PostBox
   initialize and the player is spawned (`RequestSpawnPlayerServerRpc`, once per sender).
5. Everything spawned in the scene (counters, kitchen objects, bots) reaches the late joiner with
   its current NetworkVariable state.
6. **Approval by player id.** Each machine sends its player id in the connection payload
   (`ConnectionPayload`, `NetworkConfig.ConnectionApproval = true`, set in code on both ends). The
   host's `SeatRegistry` counts live players plus seats held for 60 s after a drop against the
   4-player limit; a known player is always let back in, and one that reconnects while its old
   connection lingers replaces it.
7. **Session lifecycle.** `SessionStateMachine` (Idle → Joining → Syncing → InGame →
   Reconnecting → Ended, host: Hosting → Ended) is fed by transport events; every end goes through
   one exit (`MultiplayerManager.RunExit`: save if the host lost its connection, shut down, main
   menu, a popup with the reason). A host that quits sends `SessionEndingRpc` first so clients show
   "The restaurant has closed".
8. **Heartbeat and reconnect.** A 1 s unreliable beat each way: 5 s without one shows
   "Reconnecting…" on the client and "Away" over that avatar on the host. The transport timeout is
   60 s. A client that loses the host retries the same join code every 3 s for 60 s through the
   normal join path; the host keeps its avatar (`DontDestroyWithOwner`, `PlayerStateMachine.IsAway`,
   server-written `avatarPlayerId`) and hands it back with `ChangeOwnership`, still holding its food.
   After 60 s the food goes to the nearest free clear counter (or is destroyed) and the avatar is
   removed. A client that quits on purpose frees its seat at once.
9. **Mirrors send totals.** Coins, gems, level/EXP, inventory counts, the post-box list, daily-free
   counts and upgraded stats reach clients as resulting values, never as "+X".

## 2b. Host ads watched by a client

With other players in the room, the host's ads don't freeze the restaurant
(`GameManager.AdDelegation.cs`, rules in `AdDelegationRules`):

- **Rewarded (daily free item):** the host picks a client in "Who watches?"; the client accepts
  within 15 s or declines (60 s cooldown before it can be asked again). When nobody is left to ask
  (refused, cooling down, or no ad) the host may press "Force a random player" (switch:
  `Assets/Resources/AdDelegationConfig.asset`), or watch it itself ("Me — the restaurant pauses").
  The server grants through `ServerGrantDailyFree`; one open request at a time, 2 min expiry.
- **Interstitials:** the host's `Break_Time` rotates through the clients (in addition to their own,
  never within 90 s of a phone's previous one, at a safe moment); the host's `Level_Up` is skipped.
- Ads go through `IAdProvider` (`LevelPlayAdProvider` on devices, `FakeAdProvider` in the Editor).

## 3. Player interactions

- A click on a counter, plate or customer is sent as `PlayerStateMachine.InteractServerRpc`.
  The server checks the sender owns that avatar and is in range, then runs the target's
  `InteractEvent(actor)` on the server.
- Feedback goes to that player only: `UIManager.Instance.ShowAlert(actor, ...)` and
  `UIManager.Instance.ShowOptionMenu(actor, ...)`. UIManager is a server-owned NetworkBehaviour
  (its own NetworkObject in MainMenuScene) that targets the actor's client with
  `RpcTarget.Single`, so the player class holds no UI calls. A choice in the option menu comes
  back through `UIManager.RequestOption`; the server takes the sender's avatar as the actor,
  checks range, and the target's `IHasOptionalSO.ApplyOption(actor, index)` validates the index.
- Cooking tools: USE only opens the local close-up (no RPC). In the close-up, a drag in or out sends
  the same `InteractServerRpc`, and each finished gesture sends
  `PlayerStateMachine.ReportGestureServerRpc(tool counter, GestureFamily, value)` (owner + range
  checked). The server's `CookingTool.ApplyGesture` rejects reports for the wrong family, the wrong
  state, duplicates, and flip/manual-pour values more than 0.2 away from its own cook fraction, then
  records them in `CookStepRecorder`. Stars are computed and written only on the server
  (`KitchenObject.quality`, a server-written NetworkVariable).

## 4. Kitchen objects

- Spawned only by the server: `KitchenGameManager.SpawnKitchenObject(so, parent, index)` sets the
  parent before `Spawn`, so clients never see an unparented item.
- Who holds an item is a server-written `NetworkVariable` on `KitchenObject` (parent behaviour +
  slot index). Every peer applies it to its local parent view (counter, player hand, table seat,
  cooking tool); late joiners resolve it on spawn.
- `DestroySelf` is server-only and despawns; parents clear their slot in `OnNetworkDespawn`.
- Plate contents, eaten/served flags, table seat occupancy, container fill levels, refill-box
  ingredient, cooking state/timers and the restaurant open/closed state are all NetworkVariables
  or NetworkLists written by the server.
- There is no object pooling for kitchen objects (it conflicted with NetworkObject lifetimes).

## 5. Grid building

- Placement preview (`BuildingGhost`) is a local, non-networked copy of the prefab.
- Placing: the client checks `GridBuildingSystem.CanPlace` for instant feedback, then sends
  `GridBuildingSystem.PlaceObjectServerRpc`. The server checks again (every covered cell,
  unlocked cells, tool slots) and that the item is in the inventory, removes it from the
  inventory and spawns the object (server-owned, so it survives its builder leaving).
- Every peer registers a placed object in its own grid in `PlacedObjectView.OnNetworkSpawn` and
  removes it in `OnNetworkDespawn`. Only an explicit pickup (`PickUpPlacedObjectServerRpc`)
  removes the saved entry and returns the item to the inventory.
- Unlock/expand run on the server; `ApplyGridSizeClientRpc` is sent before anything spawns into
  the new cells. The NavMesh is rebuilt on the host after the floor changes.

## 6. Economy

All in `GameManager.Economy.cs` (a partial of `GameManager`, split from `GameManager.Save.cs`,
`GameManager.Data.cs`). Clients call intent RPCs — `BuyShopItemServerRpc`,
`ExchangeCurrencyServerRpc`, `UnlockDishServerRpc`, `PurchaseUpgradeServerRpc`,
`UnlockCosmeticServerRpc`, `ClaimDailyFreeServerRpc` — and the server checks level, price and
balance, then debits and grants in one step. Customer payments are computed on the server when a
customer finishes eating and collected once when a player picks up the plate. The payment is
`QualityRules.Payment(price, wait-time tip, plate.DishStars)`: the plate's dish stars (average of the
gesture-made ingredients) add +10 % (2 stars) or +25 % (3 stars) of the price. Level-up rewards are
granted by the server (`ServerAddExp`); reward popups are display only. Skill upgrades apply to
the player who bought them. Gem packs (IAP) can only be bought by the host.

## 7. Persistence

- Only the host saves (`GameManager.CanSave`). Changes mark the save dirty and it is written at
  most once per second, plus immediately on pause/quit and after a purchase.
- Files are written atomically (`SaveJson.WriteAtomic`: temp file, then replace, keeping
  `<file>.bak`); loading falls back to the backup. `GameData.SaveVersion` + `Migrate()` upgrade old
  saves. A type binder limits what `$type` in JSON may create (saves and the join snapshot).

## 8. Known remaining risks

- Rewarded-ad completion is reported by the client and cannot be verified by the host.
- **Forced rewarded ads (owner-accepted risk):** "Force a random player" shows a rewarded ad to a
  client who did not opt in. Rewarded ads are normally opt-in under ad-network policy
  (LevelPlay/AdMob — to be checked); turn it off with `ForceWatchWhenAllDecline` in
  `Assets/Resources/AdDelegationConfig.asset`.
- How long Unity Relay keeps a suspended phone (an ad) connected is unmeasured; the 60 s transport
  timeout and the 60 s reconnect assume it is at least that long.
- Daily reward resets use the host's clock.
- Receipt validation needs the generated `GooglePlayTangle` (IAP Receipt Validation Obfuscator).
- Ads consent is deny-by-default (`AdsManager`: no personalised ads, CCPA opt-out on) and only
  changes through `SetGdprConsent` / `SetCcpaOptOut`; a consent dialog or certified CMP still
  needs to call them.
