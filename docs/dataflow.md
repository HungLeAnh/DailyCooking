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

All in `GameManager.cs` (the economy section). Clients call intent RPCs — `BuyShopItemServerRpc`,
`ExchangeCurrencyServerRpc`, `UnlockDishServerRpc`, `PurchaseUpgradeServerRpc`,
`UnlockCosmeticServerRpc`, `ClaimDailyFreeServerRpc` — and the server checks level, price and
balance, then debits and grants in one step. Customer payments are computed on the server when a
customer finishes eating and collected once when a player picks up the plate. Level-up rewards are
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
- Daily reward resets use the host's clock.
- Receipt validation needs the generated `GooglePlayTangle` (IAP Receipt Validation Obfuscator).
- Ads consent is granted for everyone until a consent prompt is added (see `AdsManager`).
