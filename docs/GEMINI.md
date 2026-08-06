# Gemini Project Guide: DailyCooking

This document provides essential information for developers using the Gemini CLI with the DailyCooking project.

## 1. Project Overview

DailyCooking is a multiplayer cooking game built with Unity. It features a modern architecture using Unity's Netcode for GameObjects, the Universal Render Pipeline (URP), and the new Input System. The gameplay is centered around a grid-based system for placing and interacting with kitchen objects.

## 2. Key Technologies & Architectural Patterns

- **Engine:** Unity (2023.4.1f1)
- **Rendering:** Universal Render Pipeline (URP)
- **Multiplayer:** Unity Netcode for GameObjects (`com.unity.netcode.gameobjects`)
- **Input:** Unity's new Input System (`com.unity.inputsystem`)
- **Core Patterns:**
    - **Component-Based Architecture:** Standard Unity practice.
    - **State Machine:** Manages high-level game flow (e.g., Menu, Gameplay, GameOver). Located in `Assets/Scripts/StateMachine`.
    - **ScriptableObjects (SO):** Used for data-driven design (recipes, ingredients). See `Assets/Scripts/SO` for definitions and `Assets/SO` for data assets.
    - **Manager Classes:** Singleton managers for global systems. Located in `Assets/Scripts/Managers`.
    - **MVC (Model-View-Controller):** Used to structure game logic. See `Assets/Scripts/MVC`.
- **External Libraries:**
    - **RestSharp:** For making calls to external web APIs (e.g., leaderboards).

## 3. Getting Started & Workflow

### Initial Setup

1.  **Open in Unity:** Add this project to Unity Hub and open it with **Unity Editor version 2023.4.1f1**. The first open will take time to import assets.
2.  **Open Main Scene:** Load the `Assets/Scenes/MainMenu.unity` scene.
3.  **Run:** Press "Play" in the editor to start the game.

### Development Workflow

- **Branching:** Create new branches from `develop` or `main` for all new work.
    - `feature/<feature-name>`
    - `bugfix/<bug-name>`
- **Coding Style:** Follow the existing C# coding conventions found in the project.
- **Commits:** Make small, atomic commits with clear messages. Submit work via Pull Requests for review.

## 4. Important Locations

- **Core Logic:** `Assets/Scripts/`
- **Reusable GameObjects:** `Assets/Prefabs/`
- **Data Assets:** `Assets/SO/`
- **Player Controls:** `Assets/Scripts/PlayerAction.inputactions`
- **Multiplayer Scripts:** Look for files inheriting from `NetworkBehaviour`.

## 5. How to Build

*To be defined. This section should include the command-line steps to build the project for different platforms (e.g., Windows, Android).*

## 6. How to Run Tests

*To be defined. This section should include the command-line steps to execute the project's test suite.*
