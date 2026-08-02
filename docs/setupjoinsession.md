# DailyCooking — Relay Session Setup Guide

## Overview

The project has **two session paths** that are partially implemented but **not wired together**:

| Path | Transport | Status |
|---|---|---|
| Direct IP | Raw `UnityTransport` | Used by `MultiplayerManager` — works but needs IP sharing |
| Relay | `UnityTransport` + Relay SDK | Code exists in `SessionManager` — **never called** |

---

## Current Architecture

### `MultiplayerManager.cs` — UI-facing session API

Called from `UIMainMenuPopup` (`Assets/Scripts/UI/UIPopup/Popups/UIMainMenuPopup.cs`):

- **Host** (`StartHostSessionAsync()`, line 158):
  ```csharp
  networkManager.NetworkConfig.NetworkTransport = unityTransport;
  NetworkManager.Singleton.StartHost();
  ```
- **Client** (`StartClientSession()`, line 184):
  ```csharp
  networkManager.NetworkConfig.NetworkTransport = unityTransport;
  NetworkManager.Singleton.StartClient();
  ```

Both use raw `UnityTransport` — no Relay.

### `SessionManager.cs` — Relay methods (unused)

- **Host** (`StartHostWithRelay()`, line 342):
  ```csharp
  var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
  NetworkManager.Singleton.GetComponent<UnityTransport>()
      .SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
  var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
  return NetworkManager.Singleton.StartHost() ? joinCode : null;
  ```

- **Client** (`StartClientWithRelay()`, line 355):
  ```csharp
  var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
  NetworkManager.Singleton.GetComponent<UnityTransport>()
      .SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
  return NetworkManager.Singleton.StartClient();
  ```

---

## How to Wire Relay End-to-End

### 1. Host flow — update `MultiplayerManager.StartHostSessionAsync()`

Replace the direct `StartHost()` call with SessionManager's relay method and return the join code to the UI:

```csharp
public async Task<string> StartHostSessionAsync()
{
    try
    {
        if (!SessionManager.Instance.IsSignedIn())
            await SessionManager.Instance.SignInAnonymouslyAsync();

        // Subscribe server callbacks
        NetworkManager.Singleton.ConnectionApprovalCallback = NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Server_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;

        // Use Relay instead of direct IP
        string joinCode = await SessionManager.Instance.StartHostWithRelay(
            MAX_PLAYER_AMOUNT, "dtls");

        if (string.IsNullOrEmpty(joinCode))
        {
            NetworkLog.LogError("Failed to start host with Relay");
        }

        return joinCode;
    }
    catch (Exception e)
    {
        Debug.LogError($"Failed to create session: {e.Message}");
        return null;
    }
}
```

### 2. Client flow — update `MultiplayerManager.StartClientSession()`

Accept a `joinCode` parameter:

```csharp
public async Task<bool> StartClientSession(string joinCode)
{
    try
    {
        if (!SessionManager.Instance.IsSignedIn())
        {
            UIManager.Instance.ShowAlertMessage("You must be signed in to join.");
            return false;
        }

        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;

        return await SessionManager.Instance.StartClientWithRelay(joinCode, "dtls");
    }
    catch (Exception e)
    {
        Debug.LogError($"Failed to join session: {e.Message}");
        return false;
    }
}
```

### 3. UI flow — update `UIMainMenuPopup.cs`

**Host button** — display join code after creation:

```csharp
NewRestaurant.onClick.AddListener(async () =>
{
    // ... create game data ...
    string joinCode = await MultiplayerManager.Instance.StartHostSessionAsync();
    if (!string.IsNullOrEmpty(joinCode))
    {
        // Show join code to host (copied to clipboard or displayed in UI)
        GUIUtility.systemCopyBuffer = joinCode;
        UIManager.Instance.ShowAlertMessage($"Join Code: {joinCode} (copied)");
        Loader.LoadNetwork(Loader.Scene.GameScene);
        GameManager.Instance.SwitchState(new InGameState(GameManager.Instance));
        UIPopupManager.Instance.HidePopup(UIPopupType.UIMainMenuPopup);
    }
});
```

**Join button** — pass the entered join code:

```csharp
joinButton.onClick.AddListener(() =>
{
    UIPopupManager.Instance.ShowPopup(UIPopupType.UIJoinRestaurantPopup, new UIJoinRestaurantPopup.Param
    {
        OnSubmit = async (joinCode) =>
        {
            bool success = await MultiplayerManager.Instance.StartClientSession(joinCode);
            if (success)
            {
                Loader.LoadNetwork(Loader.Scene.GameScene);
                GameManager.Instance.SwitchState(new InGameState(GameManager.Instance));
                UIPopupManager.Instance.HidePopup(UIPopupType.UIMainMenuPopup);
            }
        }
    });
});
```

### 4. UIJoinRestaurantPopup — pass join code

Ensure `UIJoinRestaurantPopup.Param.OnSubmit` passes the user-entered join code string. Check the current implementation at `Assets/Scripts/UI/UIPopup/Popups/JoinRestaurantPopup/UIJoinRestaurantPopup.cs`.

---

## Key Points

- **Authentication must happen before Relay** — both host and client call `SignInAnonymouslyAsync()` first.
- **Connection type** — `"dtls"` for secure, `"wss"` for WebGL fallback.
- **`NetworkManager.Singleton` vs `networkManager`** — the relay methods in `SessionManager` use `NetworkManager.Singleton.GetComponent<UnityTransport>()` while `MultiplayerManager` uses a serialized `[SerializeField] UnityTransport unityTransport`. Both reference the same component, but be consistent to avoid confusion.
- **Server callbacks** — subscribe before `StartHost()`, unsubscribe in `ShutdownAndReset()`.
- **Client disconnect** — the existing `NetworkManager_Client_OnClientDisconnectCallback` already fires `OnFailedToJoinGame` if connection fails.
