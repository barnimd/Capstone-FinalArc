# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Capstone-FinalArc** is a WebGL-based interactive game for cybersecurity awareness education, built with **Unity 2022.3.62f3** in C#. Players navigate office environments, interact with NPCs, and complete cybersecurity-themed challenges across multiple topics.

## Build & Development

- **Unity Version:** 2022.3.62f3 (must match exactly)
- **IDE:** Visual Studio Code (configured) or Visual Studio 2022+
- **Solution file:** `Capstone-FinalArc.sln`
- **Build target:** WebGL (browser deployment)
- **Build process:** Standard Unity build pipeline — use File > Build Settings in Unity Editor
- **No CLI build scripts, test suites, or linters are configured**

## Architecture

### Game Topics (Educational Modules)
- **Topic 1:** Phishing & Social Engineering — players identify phishing emails, suspicious links, and social engineering attacks in an office setting
- **Topic 2:** Password Security & MFA — players learn password strength, multi-factor authentication through a simulated desktop environment

### Key Managers (Singleton Pattern)
- `FirebaseManager` (`Assets/Script/Firebase/`) — authentication (email/password), Firestore database, leaderboard sync
- `AuthUIManager` (`Assets/Script/UI/`) — login/signup flow, session management, scene transitions after auth
- `DialogueManager` (`Assets/Script/Bara/`) — NPC dialogue system with branching choices and scoring
- `GameplayManager` / `GameFlowManager` (`Assets/Script/Bara/`) — game state, timer, progress tracking
- `Topic2_DesktopManager` (`Assets/Script/Ghaza/`) — desktop simulation UI for Topic 2

### Script Organization
Scripts are organized by team member under `Assets/Script/`:
- `Bara/` — core game mechanics (dialogue, gameplay flow, interactions)
- `Firebase/` — Firebase integration
- `Game Topic 1/` — topic 1-specific scripts (desktop sim, email interactions, installation)
- `Ghaza/` — topic 2 features (password validation, MFA, desktop manager)
- `Sidiq/` — player movement
- `UI/` — authentication UI controllers

### Scene Structure
- **Auth scenes:** `LoginScene`, `SignUpScene`
- **Menu scenes:** Dashboard, Class, Leaderboard, Profile, GameGuideline (under `Scenes/Menu/`)
- **Game scenes:** Office prototypes under `Scenes/Game/Game Topic 1/` and `Game Topic 2/`
- Scene build order is configured in `ProjectSettings/EditorBuildSettings.asset`

### Data Flow
- `ScriptableObjects` (e.g., `DialogueData`) define NPC dialogue content and choices
- `FirebaseConfig` asset in `Assets/Resources/` holds Firebase credentials
- Player progress and scores sync to Firestore via `FirebaseManager`

## Dependencies

- **Firebase SDK** — Authentication, Firestore, Analytics
- **TextMesh Pro** — UI text rendering
- **RestClient** — HTTP requests
- **FullSerializer** — JSON serialization
- **RSG.Promise** — async/promise pattern
- **Speech Bubble** — dialogue UI plugin

## How to Work With Me (Claude Workflow)

1. **CAVEMAN MODE** — Always use simple, direct, no-fluff language. Short sentences. No filler words. No corporate speak. Just the facts and the code.
2. **Plan with Opus, Execute with Sonnet** — Use `/opus` (or the Opus model) to create a plan first. Check off each step as it is completed. Use Sonnet for the actual implementation and edits.
3. **End-of-task summary** — After finishing any task, provide a brief summary of what was done: what changed, what files were modified, and what is next.

## Conventions

- Prefabs stored in `Assets/Prefab/` (Canvas, Manager, player)
- Art assets (tilesets, characters, sprites) in `Assets/Asset game/`
- Some code comments are in Indonesian
- Private fields sometimes use underscore prefix (`_PlayerMovement`)
- MonoBehaviour-based architecture throughout
