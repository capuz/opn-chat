# opn-chat — Project Context

## Stack

- **Frontend**: React 19 + TypeScript, Vite, Tailwind CSS v4, React Router v7
- **Backend**: ASP.NET Core (.NET), SignalR hubs, JWT auth
- **Real-time**: Microsoft SignalR — two hubs: `/hubs/chat` and `/hubs/presence`
- **Auth**: Google OAuth + JWT (refresh token in localStorage)
- **Package manager**: npm

## Structure

```
opn-chat/
├── opn-chat-client/          ← React frontend
│   ├── src/
│   │   ├── pages/
│   │   │   ├── ChatPage.tsx  ← Main chat UI (theme system + SignalR)
│   │   │   ├── LoginPage.tsx ← Google OAuth + dev bypass
│   │   │   └── DashboardPage.tsx
│   │   ├── services/
│   │   │   ├── signalr.service.ts  ← Hub connection lifecycle
│   │   │   ├── auth.service.ts     ← Google OAuth, logout, token mgmt
│   │   │   ├── api.service.ts      ← Axios instance with JWT interceptors
│   │   │   ├── room.service.ts     ← Room CRUD
│   │   │   └── privateChat.service.ts
│   │   ├── hooks/useAuth.tsx  ← Auth context (user, isAuthenticated, loading, refreshAuth)
│   │   ├── types/auth.ts      ← User, Message types
│   │   ├── types/privateMessage.ts
│   │   ├── index.css          ← Global styles + chat CSS variables (--ch-*)
│   │   └── App.tsx            ← Router + ProtectedRoute
│   └── index.html
└── src/                       ← .NET backend
    ├── Domain/
    ├── Application/
    ├── Infrastructure/        ← SignalR hubs, EF Core
    └── WebAPI/                ← Controllers, ASP.NET Core config
```

## API Endpoints

```
POST /api/auth/google           ← Google OAuth exchange
POST /api/auth/refresh          ← Token refresh
POST /api/auth/logout
GET  /api/profile/me            ← nicknameChangesLeft, nickname, email
PUT  /api/profile/nickname      ← { nickname } → { changesLeft }
GET  /api/rooms/public          ← [{ id, name, description }]
POST /api/rooms                 ← { name, description, isPrivate, password? }
GET  /api/rooms/{id}/messages   ← ?take=50 → Message[]
```

## ChatPage Architecture

**Theme system** (dark/light):
- `data-theme` attribute on root div drives CSS variables (`--ch-*`)
- Dark: phosphor teal accent `#5eead4`, background `#07070e`
- Light: indigo accent `#4f46e5`, background `#f8f8fb`
- Persisted in `localStorage` key `chat-theme`, falls back to OS preference
- Toggle button (sun/moon) in header

**Cursor phosphor glow** (dark mode only):
- `div.cursor-glow` — `position: fixed`, `pointer-events: none`, `z-index: 999`
- Updated via `document.addEventListener('mousemove')` → `style.setProperty('--cx', px)` (direct DOM, no React re-render)
- CSS: `radial-gradient(600px circle at var(--cx) var(--cy), var(--ch-glow), transparent 70%)`
- `--ch-glow` is `transparent` in light mode → no effect

**SignalR flow**:
1. `useEffect` → `startConnection('chat')` → `JoinRoom(firstRoomId)` → load messages
2. `useEffect` → `startConnection('presence')` → `JoinPresenceRoom(activeRoom)` → OnlineUsersList event
3. Presence events: `OnlineUsersList`, `UserOnline`, `UserOffline`
4. Chat events: `ReceiveMessage`

**Key state**:
- `chatMode: 'global' | 'private'` — switches between room and DM views
- Sidebars hidden in private mode
- `nicknameChangesLeft` — max 3 per account, loaded from `/api/profile/me`

## CSS Variables (Chat, in index.css)

`--ch-bg`, `--ch-bg-2`, `--ch-bg-3`, `--ch-hover` — background layers
`--ch-border`, `--ch-border-2` — borders
`--ch-text`, `--ch-text-2`, `--ch-text-3` — text hierarchy
`--ch-accent`, `--ch-accent-dim` — accent color + transparent version
`--ch-me` — color for current user's name in chat
`--ch-header` — header background
`--ch-input-bg` — input field background
`--ch-online-dot`, `--ch-offline-dot` — presence indicator colors
`--ch-btn-active`, `--ch-btn-text` — primary button colors
`--ch-error` — error states
`--ch-modal-bg` — modal card background
`--ch-glow` — cursor glow color (transparent in light mode)

## Fonts

Google Fonts loaded in `index.css`:
- `DM Sans` (300, 400, 500, 600) — UI font
- `DM Mono` (300, 400, 500) — timestamps in chat

## Dev Commands

```bash
# Backend (ASP.NET Core)
cd src/WebAPI/opn-chat.WebAPI && dotnet run

# Frontend (Vite + React)
cd opn-chat-client && npm run dev
```

## Rules for this project

- **Always run backend AND frontend** after any code modification — use `run_in_background` for both
- LoginPage uses mostly hardcoded colors (not `--ch-*` variables) — intentional
- DashboardPage uses Tailwind classes
- Emoji picker (`emoji-picker-react`) theme is driven by `theme` state → `Theme.DARK` / `Theme.LIGHT`

# === COGNILAYER (auto-generated, do not delete) ===

## CogniLayer v4 Active
Persistent memory + code intelligence is ON.
ON FIRST USER MESSAGE in this session, briefly tell the user:
  'CogniLayer v4 active — persistent memory is on. Type /cognihelp for available commands.'
Say it ONCE, keep it short, then continue with their request.

## MEMORY HIERARCHY (CRITICAL — ALWAYS FOLLOW)

You have TWO memory systems. Use BOTH, but with clear priority:

### PRIMARY: CogniLayer MCP (memory_search / memory_write)
- ALWAYS use FIRST for both reading and writing
- FTS5 + vector search, heat decay, 14 fact types, code intelligence
- On-demand — loads only relevant facts (~500 tokens instead of tens of thousands)
- Store here: decisions, gotchas, patterns, error_fixes, api_contracts, procedures

### SECONDARY (FALLBACK): Auto-memory (MEMORY.md files)
- Use when CogniLayer MCP is unavailable, fails, or returns empty
- MEMORY.md is loaded into context ALWAYS at session start — keep it SHORT (max 30 lines)
- Store here only: critical user feedback, deploy workflow, 1-line pointers to CogniLayer

### RULES:
1. READING: memory_search(query) FIRST → if empty/error → read MEMORY.md files
2. WRITING: memory_write() ALWAYS → ALSO to auto-memory ONLY if critical user feedback/rule
3. NEVER duplicate content — if fact is in CogniLayer, put only a 1-line pointer in auto-memory
4. Auto-memory MEMORY.md is an INDEX, not a database — format: `- [topic] → /recall keyword`
5. If CogniLayer MCP fails → USE auto-memory as base and alert user about MCP issue

### CHECK (every ~10 prompts or before ending work):
- Did I save new findings to memory_write()? If not → save NOW
- Is session bridge current? If not → session_bridge(action="save")
- DO NOT wait for end of session — save continuously, session may crash

## Tools — HOW TO WORK

FIRST RUN ON A PROJECT:
When DNA shows "[new session]" or "[first session]":
1. Run /onboard — indexes project docs (PRD, README), builds initial memory
2. Run code_index() — builds AST index for code intelligence
Both are one-time. After that, updates are incremental.
If file_search or code_search return empty → these haven't been run yet.

UNDERSTAND FIRST (before making changes):
- memory_search(query) → what do we know? Past bugs, decisions, gotchas
- code_context(symbol) → how does the code work? Callers, callees, dependencies
- file_search(query) → search project docs (PRD, README) without reading full files
- code_search(query) → find where a function/class is defined
Use BOTH memory + code tools for complete picture. They are fast — call in parallel.

BEFORE RISKY CHANGES (mandatory):
- Renaming, deleting, or moving a function/class → code_impact(symbol) FIRST
- Changing a function's signature or return value → code_impact(symbol) FIRST
- Modifying shared utilities used across multiple files → code_impact(symbol) FIRST
- ALSO: memory_search(symbol) → check for related decisions or known gotchas
Both required. Structure tells you what breaks, memory tells you WHY it was built that way.

AFTER COMPLETING WORK:
- memory_write(content) → save important discoveries immediately
  (error_fix, gotcha, pattern, api_contract, procedure, decision)
- session_bridge(action="save", content="Progress: ...; Open: ...")
DO NOT wait for /harvest — session may crash.

## SHORT SESSIONS = BETTER PERFORMANCE
- With 200K context, session compresses sooner → faster responses
- CogniLayer bridge + memory_search replaces lost history for ~2K tokens
- After completing a coherent block of work: save bridge → suggest user starts new session
- Use /compact when session grows and work is not yet done

SUBAGENT MEMORY PROTOCOL:
When spawning Agent tool for research or exploration:
- Include in prompt: synthesize findings into consolidated memory_write(content, type, tags="subagent,<task-topic>") facts
  Assign a descriptive topic tag per subagent (e.g. tags="subagent,auth-review", tags="subagent,perf-analysis")
- Do NOT write each discovery separately — group related findings into cohesive facts
- Write to memory as the LAST step before return, not incrementally — saves turns and tokens
- Each fact must be self-contained with specific details (file paths, values, code snippets)
- When findings relate to specific files, include domain and source_file for better search and staleness detection
- End each fact with 'Search: keyword1, keyword2' — keywords INSIDE the fact survive context compaction
- Record significant negative findings too (e.g. 'no rate limiting exists in src/api/' — prevents repeat searches)
- Return: actionable summary (file paths, function names, specific values) + what was saved + keywords for memory_search
- If MCP tools unavailable or fail → include key findings directly in return text as fallback
- Launch subagents as foreground (default) for reliable MCP access — user can Ctrl+B to background later
Why: without this protocol, subagent returns dump all text into parent context (40K+ tokens).
With protocol, findings go to DB and parent gets ~500 token summary + on-demand memory_search.

BEFORE DEPLOY/PUSH:
- verify_identity(action_type="...") → mandatory safety gate
- If BLOCKED → STOP and ask the user
- If VERIFIED → READ the target server to the user and request confirmation

## VERIFY-BEFORE-ACT
When memory_search returns a fact marked ⚠ STALE:
1. Read the source file and verify the fact still holds
2. If changed → update via memory_write
3. NEVER act on STALE facts without verification

## Process Management (Windows)
- NEVER use `taskkill //F //IM node.exe` — kills ALL Node.js INCLUDING Claude Code CLI!
- Use: `npx kill-port PORT` or find PID via `netstat -ano | findstr :PORT` then `taskkill //F //PID XXXX`

## Git Rules
- Commit often, small atomic changes. Format: "[type] what and why"
- commit = Tier 1 (do it yourself). push = Tier 3 (verify_identity).

## Project DNA: opn-chat
Stack: TypeScript
Style: [unknown]
Structure: .agents, .atl, .vscode, opn-chat-client, src
Deploy: [NOT SET]
Active: [new session]
Last: [first session]

## Session Continuity
State: opn-chat-client/index.html (edit)
Files: opn-chat-client/src/locales/pt-BR/monetize.json, opn-chat-client/src/locales/es/monetize.json, opn-chat-client/src/locales/en/monetize.json, opn-chat-client/src/components/RewardModal.tsx, opn-chat-client/src/index.css

# === END COGNILAYER ===
