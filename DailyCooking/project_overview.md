# DailyCooking Project Overview

## Project Summary
**DailyCooking** is a multiplayer cooking simulation game built using **Unity 2023.4.1f1**. The project follows a highly structured, data-driven architecture to manage gameplay complexity, networked state synchronization, and UI orchestration.

## Core Architecture
The project adheres to several key architectural patterns to ensure scalability and maintainability:

1.  **Manager-Based Singleton Pattern:**
    Centralized game systems are managed via `MonoBehaviour` singletons located in `Assets/Scripts/Managers/`. These manage specific game states (e.g., `GameManager`, `KitchenGameManager`), multiplayer sessions, and economic systems (e.g., `ShopManager`, `UpgradeManager`).
2.  **State Machine (FSM):**
    Two distinct Finite State Machine layers manage game flow and player actions:
    *   **Game-Level:** Handles transitions between major game states like `MainMenu` and `InGame`.
    *   **Player-Level:** Manages player movement and interaction states (e.g., `Idle`, `Walk`).
3.  **Data-Driven Design (ScriptableObjects):**
    Game content (recipes, kitchen objects, upgrades) is decoupled from logic using ScriptableObjects (`Assets/Scripts/SO/`). This allows for balancing and content expansion without altering core code.
4.  **MVC (Model-View-Controller):**
    Kitchen interactions (counters, cooking tools) are structured using an MVC pattern to separate interaction logic (Controllers) from data and visual feedback.
5.  **Multiplayer Networking:**
    The project utilizes **Netcode for GameObjects**. It follows a strict Server/Host-authoritative model, where state changes are requested via `ServerRpc` and synchronized across clients.

## Technology Stack
*   **Engine/Rendering:** Unity 2023.4.1f1 with Universal Render Pipeline (URP).
*   **Networking:** Netcode for GameObjects.
*   **Input:** Unity Input System (via `GameInput` wrapper).
*   **AI/Tools Integration:** An active MCP (Model Context Protocol) bridge is implemented on HTTP port 8080, facilitating AI-driven development and editor management.

## Project Organization
The `Assets/` directory is organized by functionality:
*   **`Scripts/`**: Contains the core logic separated by concern (Managers, FSM, MVC, Network, Player, SO definitions).
*   **`SO/`**: Houses the instantiated ScriptableObject data assets.
*   **`Prefabs/`**: Stores reusable GameObjects.
*   **`Scenes/`**: Contains key scenes (`MainMenu`, `GameScene`, `TestScene`).
