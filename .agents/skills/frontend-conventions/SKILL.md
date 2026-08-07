---
name: frontend-conventions
description: Skill for frontend development conventions in PrepChess — React 19 + TypeScript + Vite 8 patterns, project structure, component guidelines, state management, styling, and routing.
---

# Frontend Conventions Skill

## Tech Stack

- **Vite 8** — Build tool and dev server
- **React 19** — UI library (with React Compiler via `babel-plugin-react-compiler`)
- **TypeScript 6** — Type safety (strict mode)
- **React Router DOM 7** — Client-side routing
- **CSS Modules** — Scoped component styles

## Project Structure (Planned)

```
frontend/src/
├── assets/                # Static assets (images, fonts, icons)
├── components/            # Shared/reusable components
│   ├── ui/                # Generic UI primitives (Button, Modal, Card, etc.)
│   └── chess/             # Chess-specific components (Board, Piece, Square, etc.)
├── hooks/                 # Custom React hooks
│   ├── useSignalR.ts      # SignalR connection management
│   ├── useGame.ts         # Game state hook
│   ├── useAuth.ts         # Authentication state
│   └── useTimer.ts        # Clock/timer logic
├── pages/                 # Route-level page components
│   ├── Home/
│   ├── Game/
│   ├── Lobby/
│   ├── Profile/
│   ├── DeckBuilder/
│   └── Login/
├── services/              # API and external service wrappers
│   ├── api.ts             # Typed fetch wrapper
│   └── signalr.ts         # SignalR connection builder
├── types/                 # Shared TypeScript types and interfaces
│   ├── game.ts            # Game, Move, Fen types
│   ├── match.ts           # Match, Deck, OpeningCard types
│   └── user.ts            # User, Rating types
├── utils/                 # Pure utility functions
├── App.tsx                # Root component with router
├── main.tsx               # Entry point (ReactDOM.createRoot)
└── index.css              # Global styles and design tokens
```

### Co-location Rules
- Page-specific components live inside their page folder: `pages/Game/MoveHistory.tsx`
- Shared components live in `components/`
- If a component is used by 2+ pages, move it to `components/`

---

## Component Patterns

### Functional Components Only

No class components. Always use function declarations with named exports:

```tsx
import type { ReactNode } from 'react'

interface GameBoardProps {
  fen: string
  onMove: (from: string, to: string) => void
  isPlayerTurn: boolean
}

export function GameBoard({ fen, onMove, isPlayerTurn }: GameBoardProps) {
  return (
    <div className={styles.board}>
      {/* ... */}
    </div>
  )
}
```

### Rules
- **Named exports only** — `export function X` not `export default function X`
- **Props interface** — Always define `{ComponentName}Props` interface above the component
- **No `React.FC`** — Use plain function declarations (React 19 doesn't need it)
- **Destructure props** — In the function signature, not inside the body

### Children Pattern

```tsx
interface LayoutProps {
  children: ReactNode
  title: string
}

export function Layout({ children, title }: LayoutProps) {
  return (
    <main>
      <h1>{title}</h1>
      {children}
    </main>
  )
}
```

---

## Hooks

### Naming
- Always prefix with `use`: `useGame`, `useSignalR`, `useAuth`
- Place in `hooks/` directory
- One hook per file

### Custom Hook Pattern

```tsx
import { useState, useEffect } from 'react'

interface UseGameReturn {
  fen: string
  isPlayerTurn: boolean
  makeMove: (from: string, to: string) => void
  isLoading: boolean
}

export function useGame(gameId: string): UseGameReturn {
  const [fen, setFen] = useState('')
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    // Connect to game via SignalR, load initial state
  }, [gameId])

  const makeMove = (from: string, to: string) => {
    // Send move via SignalR
  }

  return { fen, isPlayerTurn: true, makeMove, isLoading }
}
```

### Rules
- Prefer composition over prop-drilling — extract shared logic into hooks
- Hooks that manage SignalR connections must handle cleanup in the effect's return function
- Never call hooks conditionally

---

## State Management

### Strategy (No Redux)

| State Type      | Solution                          | Example                            |
| :-------------- | :-------------------------------- | :--------------------------------- |
| Server state    | Custom hooks + SignalR            | Game state, match data             |
| UI state        | `useState` / `useReducer`        | Modal open/closed, selected tab    |
| Global state    | React Context                    | Auth state, theme, user profile    |
| URL state       | React Router DOM                 | Current page, game ID, filters     |
| Form state      | `useState` or controlled inputs  | Login form, deck builder           |

### Context Pattern

```tsx
import { createContext, useContext, useState, type ReactNode } from 'react'

interface AuthContextType {
  user: User | null
  login: (token: string) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)

  const login = (token: string) => { /* ... */ }
  const logout = () => { /* ... */ }

  return (
    <AuthContext value={{ user, login, logout }}>
      {children}
    </AuthContext>
  )
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within AuthProvider')
  return context
}
```

---

## API Layer

### Typed Fetch Wrapper

```typescript
const API_BASE = import.meta.env.VITE_API_URL ?? 'https://localhost:5001/api'

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Request failed' }))
    throw new ApiError(response.status, error.message)
  }

  return response.json() as Promise<T>
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
}
```

---

## Styling

### CSS Modules

Every component gets a co-located `.module.css` file:

```
components/chess/
├── GameBoard.tsx
└── GameBoard.module.css
```

Usage:
```tsx
import styles from './GameBoard.module.css'

export function GameBoard() {
  return <div className={styles.board}>{/* ... */}</div>
}
```

### Rules
- **No inline styles** — Use CSS Modules or global CSS
- **No CSS-in-JS** — Keep styles in `.module.css` files
- **Design tokens** — Define colors, spacing, typography in `index.css` as CSS custom properties
- **Responsive-first** — Use `min-width` media queries (mobile-first)
- **Class composition** — Use `composes` in CSS Modules for shared styles

### Design Tokens (`index.css`)

```css
:root {
  /* Colors */
  --color-primary: hsl(220, 70%, 55%);
  --color-secondary: hsl(160, 60%, 45%);
  --color-background: hsl(220, 15%, 8%);
  --color-surface: hsl(220, 15%, 12%);
  --color-text: hsl(220, 10%, 95%);
  --color-text-muted: hsl(220, 10%, 60%);

  /* Chess board */
  --color-square-light: hsl(35, 30%, 80%);
  --color-square-dark: hsl(35, 40%, 45%);
  --color-square-highlight: hsla(50, 100%, 50%, 0.4);
  --color-square-legal: hsla(120, 80%, 50%, 0.3);

  /* Spacing */
  --space-xs: 0.25rem;
  --space-sm: 0.5rem;
  --space-md: 1rem;
  --space-lg: 1.5rem;
  --space-xl: 2rem;
  --space-2xl: 3rem;

  /* Typography */
  --font-sans: 'Inter', system-ui, sans-serif;
  --font-mono: 'JetBrains Mono', monospace;

  /* Border radius */
  --radius-sm: 0.25rem;
  --radius-md: 0.5rem;
  --radius-lg: 0.75rem;

  /* Transitions */
  --transition-fast: 150ms ease;
  --transition-normal: 250ms ease;
}
```

---

## Routing

### React Router DOM 7

```tsx
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { lazy, Suspense } from 'react'

const Home = lazy(() => import('./pages/Home/Home').then(m => ({ default: m.Home })))
const Game = lazy(() => import('./pages/Game/Game').then(m => ({ default: m.Game })))
const Login = lazy(() => import('./pages/Login/Login').then(m => ({ default: m.Login })))

export function App() {
  return (
    <BrowserRouter>
      <Suspense fallback={<LoadingSpinner />}>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/game/:gameId" element={<Game />} />
          <Route path="/login" element={<Login />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  )
}
```

### Rules
- Lazy-load all page components
- Use layout routes for shared headers/footers
- Game ID in URL: `/game/:gameId`
- Match ID in URL: `/match/:matchId`

---

## TypeScript Rules

- **Strict mode** is enabled (via `tsconfig.app.json`)
- Use `interface` for object shapes and props, `type` for unions and intersections
- **No `any`** — use `unknown` and narrow with type guards
- Use `as const` for literal tuples and constant objects
- Prefer `enum` for domain constants shared with the backend:
  ```typescript
  export enum GameStatus {
    WaitingForPlayers = 'WaitingForPlayers',
    InProgress = 'InProgress',
    Completed = 'Completed',
    Abandoned = 'Abandoned',
  }
  ```
- Import types with `import type` when only used for type checking:
  ```typescript
  import type { Game, Move } from '../types/game'
  ```
