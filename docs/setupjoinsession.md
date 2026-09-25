# DailyCooking — Relay Session Setup

Hosting and joining go through Unity Relay; players share a join code instead of an IP address.

## Code path

| Step | Code |
|---|---|
| Sign in | `SessionManager` (Unity Authentication: anonymous, Google Play Games, Unity Player Accounts) |
| Host | `UIMainMenuPopup` → `GameManager.NewGame/LoadGame` → `MultiplayerManager.StartHostSessionAsync` → `SessionManager.StartHostWithRelay(MAX_PLAYER_AMOUNT, "dtls")` → join code copied to the clipboard → `Loader.LoadNetwork(GameScene)` |
| Join | `UIJoinRestaurantPopup` → `MultiplayerManager.StartClientSession(joinCode)` → `SessionManager.StartClientWithRelay(joinCode, "dtls")`; the client follows the host's scene |
| Approval | `MultiplayerManager.NetworkManager_ConnectionApprovalCallback` refuses a 5th player ("Room full") |
| Handshake | the client sends name and player id, then requests the game data; the host sends it gzipped in chunks (see `docs/dataflow.md` §2) |
| Leave | `MultiplayerManager.ShutdownAndReset` (flushes the host's save, removes callbacks, shuts NetworkManager down) |

## Requirements

- The project must be linked to a Unity Cloud project with Relay enabled
  (Edit → Project Settings → Services).
- Players must be signed in before hosting or joining (hosting signs in anonymously if needed).
- `NetworkManager` uses the `UnityTransport` on the same GameObject; the Relay server data is set
  on it before `StartHost`/`StartClient`.

## Testing locally

Use Multiplayer Play Mode (Window → Multiplayer → Multiplayer Play Mode): start the host in the
main Editor, copy the join code from the console/alert, and join from a virtual player.
Test a late joiner too (a 3rd player joining a running restaurant).
