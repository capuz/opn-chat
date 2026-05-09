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
