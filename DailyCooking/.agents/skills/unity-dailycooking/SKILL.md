---
name: unity-dailycooking
description: >
  Project-specific knowledge and workflow guidance for the DailyCooking Unity game.
  Activates when working on Unity C# scripts, scenes, prefabs, ScriptableObjects,
  multiplayer networking, MCP tools, or any task related to the DailyCooking project.
---

# DailyCooking Unity Project — Skill

## Project Overview

DailyCooking is a **multiplayer cooking game** built with Unity 2023.4.1f1.  
Players build a restaurant, cook food using a grid-based kitchen system, and play cooperatively.

---

## 🔧 Tech Stack

| Technology | Package / Version |
|---|---|
| Engine | Unity 2023.4.1f1 |
| Rendering | Universal Render Pipeline (URP) 17.3.0 |
| Multiplayer | Netcode for GameObjects 2.9.2 |
| Input | Unity Input System 1.18.0 |
| AI Bridge | MCP for Unity (CoplayDev, HTTP on port 8080) |
| JSON | Newtonsoft.Json (com.unity.nuget.newtonsoft-json) |
| HTTP Client | RestSharp (for leaderboard / external API calls) |
| Animation | DOTween, Animation Rigging |
| IAP | Unity Purchasing 5.1.2 |

---

## 📁 Key Locations

```
Assets/
├── Editor/                    # Custom Editor tools (incl. EditorPrefsMCPTool.cs)
├── Scenes/
│   ├── MainMenuScene.unity    # Entry point — load this first when testing menu flow
│   ├── GameScene.unity        # In-game gameplay scene
│   └── TestScene.unity        # Sandbox for isolated testing
├── Scripts/
│   ├── Managers/              # Singleton game managers
│   │   ├── GameManager.cs     # Central game state orchestrator
│   │   ├── ShopManager.cs     # Shop/upgrade purchasing logic
│   │   ├── TutorialManager.cs # Tutorial flow control
│   │   ├── UpgradeManager.cs  # Kitchen upgrade logic
│   │   ├── UIManager.cs       # Top-level UI orchestrator
│   │   ├── EmotionManager.cs  # Customer emotion/satisfaction
│   │   └── States/            # GameManager FSM states
│   │       ├── MainMenuState.cs
│   │       └── InGameState.cs
│   ├── StateMachine/          # Generic FSM framework
│   │   ├── IState.cs
│   │   ├── BaseState.cs
│   │   ├── StateManager.cs
│   │   └── SubStateMachine.cs
│   ├── MVC/                   # Model-View-Controller for kitchen objects
│   │   ├── Controllers/CounterControllers/
│   │   ├── Interfaces/
│   │   └── Modules/
│   ├── SO/                    # ScriptableObject definitions
│   │   ├── RecipeSO/          # CookingRecipeSO, CuttingRecipeSO, FryingRecipeSO, BurningRecipeSO
│   │   ├── KitchenObjectSO/   # Data for each food/tool item
│   │   ├── InventorySO/       # Player inventory data
│   │   ├── UpgradeSO/         # Upgrade tier definitions
│   │   └── AudioClipRefsSO.cs
│   ├── Counters/              # Cooking mechanics
│   │   ├── KitchenGameManager.cs  # In-game cooking round manager
│   │   ├── CookingTool.cs         # Base cooking tool logic
│   │   ├── PanCookingTool.cs      # Pan-specific frying logic
│   │   └── PotCookingTool.cs      # Pot-specific boiling/stewing logic
│   ├── Player/                # Player state machine & input
│   │   ├── PlayerStateMachine.cs
│   │   ├── PlayerStateContext.cs
│   │   ├── PlayerStateFactory.cs
│   │   ├── PlayerBaseState.cs
│   │   ├── PlayerIdleState.cs
│   │   ├── PlayerWalkState.cs
│   │   ├── GameInput.cs           # Input System wrapper
│   │   └── PlayerAction.inputactions
│   ├── Network/               # Netcode multiplayer
│   │   ├── MultiplayerManager.cs
│   │   └── SessionManager.cs
│   ├── GridBuildingSystem/    # Grid-based kitchen placement
│   ├── KitchenObject/         # Pickable/placeable kitchen objects
│   ├── Interactables/         # Interactable objects
│   ├── Common/                # Shared utilities
│   ├── Bot/                   # AI bot players
│   ├── Camera/                # Camera controllers
│   ├── UI/                    # UI scripts
│   └── Configs/               # Runtime configuration
├── SO/                        # ScriptableObject data assets (instances)
├── Prefabs/                   # Reusable GameObjects
└── Packages/manifest.json     # com.coplaydev.unity-mcp added
```

---

## 🏗️ Architecture Patterns

### 1. Singleton Managers
All major systems are singleton `MonoBehaviour` managers in `Assets/Scripts/Managers/`.  
Look for `Instance` static property. Do **not** create multiple instances.

Key managers:
- `GameManager` — top-level FSM, owns game state, persists across scenes
- `KitchenGameManager` — manages a single cooking round (timers, orders, scoring)
- `MultiplayerManager` — wraps Netcode session creation/joining
- `SessionManager` — handles player sessions, role assignment
- `ShopManager`, `UpgradeManager` — economy systems
- `TutorialManager` — step-by-step tutorial flow
- `UIManager` — top-level panel/modal management

### 2. State Machine (FSM)
Two FSM layers:
- **Game-level FSM** (`GameManager`) — `MainMenuState` → `InGameState`
- **Player-level FSM** (`PlayerStateMachine`) — `PlayerIdleState` ↔ `PlayerWalkState`

Base class: `BaseState` (inherits `IState`).  
Always transition via `StateManager.SwitchState()`.

### 3. ScriptableObjects (Data-Driven Design)
All game data lives in `Assets/SO/` as ScriptableObject assets.  
Definitions (C# classes) are in `Assets/Scripts/SO/`.

| SO Type | Purpose |
|---|---|
| `CookingRecipeSO` | Input → Output ingredient mapping with cook time |
| `CuttingRecipeSO` | Cutting input/output and cut count |
| `FryingRecipeSO` | Frying time and result |
| `BurningRecipeSO` | Overcooked result |
| `KitchenObjectSO` | Ingredient/tool data (name, prefab, sprite) |
| `UpgradeSO` | Upgrade tier cost and stat delta |
| `AudioClipRefsSO` | All audio clip references |

### 4. MVC (Kitchen Objects)
Counter-based kitchen interactions use MVC:
- **Controllers** in `MVC/Controllers/CounterControllers/` — handle player interaction
- **Modules** in `MVC/Modules/` — reusable behaviour components
- **Interfaces** in `MVC/Interfaces/` — `IInteractable` defines `Interact(Player)`

### 5. Netcode for GameObjects
All networked objects inherit from `NetworkBehaviour`.  
Key rule: **only the server/host** authorizes state changes. Clients request via `ServerRpc`.  
Find all network scripts by searching for `NetworkBehaviour` inheritance.  
`MultiplayerManager` owns `NetworkManager` lifecycle.  
`SessionManager` handles player list, character assignment, and ready-up flow.

---

## 🎮 Input System
Input is handled by the new **Unity Input System**.  
- Action map defined in `Assets/Scripts/PlayerAction.inputactions`
- Generated C# class: `PlayerAction.cs` (auto-generated, do **not** edit manually)
- Wrapper: `GameInput.cs` — subscribe to events here

---

## 🔌 MCP for Unity (AI Bridge)

The project has an active MCP bridge on **HTTP port 8080**.

| EditorPrefs Key | Value | Meaning |
|---|---|---|
| `MCPForUnity.UseHttpTransport` | `1` (true) | HTTP mode active |
| `MCPForUnity.SetupCompleted` | `1` (true) | Setup wizard done |
| `MCPForUnity.LastStdIoUpgradeVersion` | `9.7.3` | Installed version |

Config: [`.agents/mcp_config.json`](file:///d:/Unity/Unity%20Project/DailyCooking/DailyCooking/.agents/mcp_config.json)
```json
{ "mcpServers": { "UnityMCP": { "url": "http://127.0.0.1:8080/mcp" } } }
```

### Custom MCP Tool: `manage_editorprefs`
Defined in [`Assets/Editor/EditorPrefsMCPTool.cs`](file:///d:/Unity/Unity%20Project/DailyCooking/DailyCooking/Assets/Editor/EditorPrefsMCPTool.cs)

Actions: `get` | `set` | `delete` | `has`  
Types: `string` | `int` | `float` | `bool`

---

## 🛠️ Development Workflow

### Branching
```
main          → stable releases
develop       → integration branch
feature/<name>
bugfix/<name>
```

### Adding a New Cooking Counter
1. Create a new `MonoBehaviour` controller in `MVC/Controllers/CounterControllers/`
2. Implement `IInteractable`
3. Create a Prefab in `Assets/Prefabs/`
4. Add a `KitchenObjectSO` data asset in `Assets/SO/` if it handles a new ingredient
5. Register the counter in the scene or `GridBuildingSystem`

### Adding a New Recipe
1. Create a new SO asset in `Assets/SO/` using the appropriate `RecipeSO` type
2. Set input ingredient, output ingredient, and timing
3. Add it to the recipe list in the relevant `CookingTool` or `KitchenGameManager`

### Adding a New Manager
1. Create the class in `Assets/Scripts/Managers/` as a singleton `MonoBehaviour`
2. Implement `IGameManager` if it needs lifecycle hooks
3. Register with `GameManager` if it needs to be initialized at startup

### Running the Game
- Open `Assets/Scenes/MainMenuScene.unity`
- Press **Play** in the Unity Editor
- For multiplayer, use **Unity Multiplayer Playmode** (Window > Multiplayer > Playmode)

---

## ⚠️ Common Pitfalls

- **Do not call `EditorPrefs` from background threads** — causes `UnityException`
- **Do not modify `PlayerAction.cs`** — it's auto-generated from `PlayerAction.inputactions`
- **Always use `ServerRpc` for state changes** in networked play — never modify `NetworkVariable` directly from a client
- **ScriptableObject data is shared** at runtime — clone with `Instantiate()` if you need per-instance mutation
- **`KitchenGameManager` is scene-scoped** (not persistent), unlike `GameManager` which is DontDestroyOnLoad

---

## 🗂️ Useful MCP Commands (when Unity is open)

```
# Read the Unity console for errors
read_console(types=["error","warning"])

# Refresh assets after file changes
refresh_unity()

# Check EditorPrefs
manage_editorprefs(action="get", key="MCPForUnity.UseHttpTransport", type="bool")

# Find a specific GameObject
find_gameobjects(name="GameManager")

# Read the active scene hierarchy
manage_scene(action="get_hierarchy")
```
