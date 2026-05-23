# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Capstone-FinalArc** (deployed as **SecMind**) is a WebGL-based interactive 2D pixel game for cybersecurity awareness education, built with **Unity 2022.3.62f3** in C#. Players navigate office environments, interact with NPCs, and complete cybersecurity-themed challenges across multiple stages.

- **Hosting:** Vercel (project domain: `secmind.vercel.app`)
- **Auth:** Firebase (email/password + Google)
- **Database:** Neon (Postgres 17 serverless) — replacing the old Firestore setup
- **Backend:** Vercel API Routes under `Docs/api/` (root dir for Vercel = `Docs/`)

## Build & Development

- **Unity Version:** 2022.3.62f3 (must match exactly)
- **IDE:** Visual Studio Code (configured) or Visual Studio 2022+
- **Solution file:** `Capstone-FinalArc.sln`
- **Build target:** WebGL → output to `Docs/` folder
- **Build process:** Standard Unity build pipeline — File > Build Settings in Unity Editor
- **No CLI build scripts, test suites, or linters are configured**

### Build Workflow Gotcha
Unity WebGL build outputs to `Docs/` and **can overwrite backend files** (`api/`, `lib/`, `package.json`). Safe process:
1. `git status` → ensure clean
2. Commit pre-build snapshot
3. Run Unity Build → WebGL to `Docs/`
4. Verify backend files still exist (`Docs/api`, `Docs/lib`, `Docs/package.json`)
5. If missing: `git checkout HEAD -- Docs/api Docs/lib Docs/package.json`
6. Commit and push the WebGL build update

## Architecture

### Game Topics (Stages)
Stage IDs use lowercase + hyphen everywhere (URL, DB, JSON, Unity code):
- **Topic 1 — `phishing`:** Phishing & Social Engineering (email & installer scams)
- **Topic 2 — `2fa`:** Password Security & MFA (simulated desktop)
- **Topic 3 — `password-security`:** Email/inbox interaction with social engineering questions
- **Topic 4 — `malware-awareness`:** URL/website login awareness, popup decisions
- **Topic 5:** Wifi selector + website security (HTTPS/cert validation)
- **Topic 6:** Ransomware & backup (file system, drag-drop trash, crash recovery)

> Note: backend `STAGES` config currently lists 4 stage IDs (`phishing`, `2fa`, `password-security`, `malware-awareness`) — Topic 5 & 6 stage IDs TBD.

### Key Managers (Singleton Pattern)
- `FirebaseManager` (`Assets/!Script/Firebase/`) — authentication, ID token retrieval
- `AuthUIManager` (`Assets/!Script/UI/`) — login/signup flow, scene transitions
- `DialogueManager` (`Assets/!Script/Bara/`) — NPC dialogue with branching choices
- `GameManager` / topic-specific `GameManager_Tp{N}` — per-topic game state, timer, progress
- `Topic2_DesktopManager` (`Assets/!Script/Ghaza/`) — desktop sim UI for Topic 2
- `EmailManager` / `EvaluationManager` (`Assets/!Script/Game Topic 3/`) — inbox & quiz flow
- `EvaluationManager_Tp4`, `EvaluationManager_Tp6` — per-topic evaluation
- `FileSystemManager`, `RansomwareController`, `BackupController` (Topic 6)

### Script Organization
Scripts under `Assets/!Script/` (note the `!` prefix, organized by team member / topic):
- `Bara/` — core mechanics (dialogue, interaction, scene management, player jump/movement)
- `Firebase/` — Firebase integration (`FirebaseManager`, `FirebaseConfig`, `FirebaseAnalytics`)
- `Game Topic 1/` — phishing/installation scripts + VN system
- `Game Topic 3/` — email interaction + topic 3 gameplay
- `Game Topic 4/` — URL awareness, popups, website login
- `Game Topic 5/` — wifi/website security
- `Game Topic 6/` — ransomware, backup, file system
- `Ghaza/` — Topic 2 (password validation, MFA, desktop)
- `Sidiq/` — player movement (top-down + side), summary
- `UI/` — auth UI, login/signup controllers, dashboard, class page, lessons
- `Editor/` — editor-only helpers (`RebuildIdleBlendTree`, per-topic scene setup)

### Scene Structure (under `Assets/!Scenes/`)
- **Auth:** `Authorization/LoginScene.unity`, `SignUpScene new.unity`
- **Menu:** `Menu/Dashboard`, `Class`, `Leaderboard`, `Profile`, `Settings`, `GameGuideline`, `GetHelp`, `InstructionGame_1`, `All_Menu`
- **Game Topic 1:** `Office_Level1_Prototype2`, `Chat_Desktop`, `Installation_Desktop`, `Privasi_Keamanan`
- **Game Topic 2:** `Office_Environment`, `Desktop_Interface`, `Website_Interface`
- **Game Topic 3:** `Map_Topic_3`, `Email_Layout`, `Testing player interaction and movement`
- **Game Topic 4:** `Map_Topic4`, `Computer_Interaction`
- **Game Topic 5:** `Map_Topic_5`, `Computer_Interaction`
- **Game Topic 6:** `computer_interaction`
- **Testing:** `playerInteraction`, `teskoneksi FIrebase`
- Scene build order: `ProjectSettings/EditorBuildSettings.asset`

### Data Flow (new architecture)
```
Unity WebGL → Firebase Auth (ID token)
            ↓ HTTPS w/ Bearer token
Vercel API (Docs/api/*) → verifyToken() → Neon Postgres
            ↓
Unity WebGL ← JSON response
```
- `ScriptableObjects` (`DialogueData`, `LessonData`, `EmailData`, `EvaluationData`, etc.) hold content
- `FirebaseConfig` asset in `Assets/Resources/` holds Firebase credentials
- Player progress, checkpoints, scores → Neon via Vercel API (Firestore is being phased out)
- Local cache: `PlayerPrefs` for instant saves + offline fallback

## Backend (Docs/ = Vercel Root)

**Local state (current):** `Docs/lib/auth.js` exists; `Docs/api/` is empty — API endpoints from the Notion plan are NOT yet created locally. Per the Notion handoff doc, they should be implemented in this order.

### Planned Endpoints (per Notion plan)
| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/api/user/sync` | POST | ✅ | Upsert user on login |
| `/api/stages` | GET | ❌ | List all stages |
| `/api/checkpoint/save` | POST | ✅ | Save checkpoint |
| `/api/checkpoint/load` | GET | ✅ | Load checkpoint + status |
| `/api/stage/complete` | POST | ✅ | Mark stage complete |
| `/api/stage/restart` | POST | ✅ | Clear checkpoint manually |
| `/api/score/submit` | POST | ✅ | Submit replay score |
| `/api/leaderboard` | GET | ❌ | Top N scores |

### Neon Schema (deployed)
Tables: `users`, `scores`, `checkpoints`, `stage_completions`. Indexes for leaderboard lookups, `UNIQUE(user_id, stage_id)` on checkpoints, `PRIMARY KEY(user_id, stage_id)` on completions.

### Env Vars (Vercel)
- `DATABASE_URL` (Neon pooled connection, auto-injected via integration)
- `FIREBASE_PROJECT_ID`, `FIREBASE_CLIENT_EMAIL`, `FIREBASE_PRIVATE_KEY`

### Backend deps (`Docs/package.json`)
`@neondatabase/serverless`, `firebase-admin`. ESM module (`"type": "module"`).

## Dependencies (Unity)

- **Firebase SDK** — Authentication (Firestore being phased out)
- **TextMesh Pro** — UI text
- **RestClient** — HTTP requests (may be superseded by `UnityWebRequest` wrapper)
- **FullSerializer**, **RSG.Promise**
- **Speech Bubble** — dialogue UI plugin

## How to Work With Me (Claude Workflow)

1. **CAVEMAN MODE** — Simple, direct, no-fluff language. Short sentences. No corporate speak.
2. **Plan with Opus, Execute with Sonnet** — `/opus` to plan first, check off steps as completed. Sonnet for implementation.
3. **End-of-task summary** — Brief summary after every task: what changed, files modified, what's next.
4. **Use unityMCP for direct Unity queries** — When inspecting Unity Editor state (active scene, hierarchy, GameObjects, components, packages, console, prefabs, etc.) ALWAYS use the `mcp__unityMCP__*` tools. Do NOT use coplay-mcp (its plugin is not installed in this project). Common ones:
   - `mcp__unityMCP__manage_scene` (get_active, get_hierarchy, get_loaded_scenes, load, save)
   - `mcp__unityMCP__manage_gameobject`, `manage_components`, `manage_prefabs`
   - `mcp__unityMCP__manage_editor` (play/pause/stop, undo/redo)
   - `mcp__unityMCP__read_console`, `manage_script`, `manage_asset`
5. **Database / backend reference** — Full migration & feature plan lives in Notion: [Plan Database Neon](https://www.notion.so/Plan-Database-Neon-2694045038d58080ab86e7fc376cc078). Fetch this when working on Neon, Vercel API routes, checkpoints, or leaderboard.

## Unity WebGL Constraints (important for backend integration)

- **No multi-threading** — use Coroutines, not `async Task`
- **No raw TCP** — only HTTPS via `UnityWebRequest`
- **`UnityWebRequest.Post(url, json)` bug** — body gets URL-encoded. Use manual construction with `UploadHandlerRaw` + `Content-Type: application/json`
- **Firebase ID token expires after 1 hour** — call `user.TokenAsync(true)` to force-refresh on 401
- **CORS not an issue** — Unity build and API share `secmind.vercel.app` domain
- **JSON library** — `JsonUtility` does not handle dictionaries/arbitrary structures; use Newtonsoft.Json for JSONB payloads

## Conventions

- Prefabs in `Assets/!Prefab/` (Canvas, Manager, player)
- Art assets in `Assets/Asset game/`
- Some code comments are in Indonesian
- Private fields sometimes use underscore prefix (`_PlayerMovement`)
- MonoBehaviour-based architecture throughout
- Stage IDs: lowercase + hyphen (`phishing`, `password-security`)
- Firebase service account JSON is `.gitignore`d — never commit

## Game Flow Decisions (locked in)

- **Replay allowed:** players can replay completed stages to improve score
- **Leaderboard:** only highest score per user per stage
- **Checkpoint on replay:** if player has a checkpoint, they continue from it
- **Manual restart:** explicit "Restart from beginning" → `/api/stage/restart` → deletes checkpoint
- **Anti-cheat:** server validates `score ≤ maxScore` per stage on every submission
