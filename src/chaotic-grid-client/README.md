# Chaotic Grid - Client Architecture

The frontend is an **Angular 19** Single Page Application (SPA). Its primary design goal is **Simplicity** and **Resilience**. It is a visual projection of the server's state.

## Architectural Guidelines

### 1. "Dumb" UI Components
Components should not make decisions.
- **Bad:** `if (user.role === 'admin') { showButton() }`
- **Good:** `if (state.permissions.canDelete) { showButton() }`
  Components receive input via **Angular Signals** and emit output via standard Events.

### 2. Signal-Based State Management
We use a centralized Signal Store to hold the current state of the board.
1. **Action:** User clicks a button.
2. **Command:** Client invokes a SignalR Hub method (`hub.invoke('Vote', ...)`).
3. **Wait:** The client *does nothing* to its local model.
4. **Reaction:** The server processes the command and broadcasts a `StateUpdated` event.
5. **Render:** The client updates the Signal Store, and the UI automatically re-renders.

### 3. Connectivity Handling
- **Reconnection:** If the SignalR connection drops, the client automatically attempts to reconnect.
- **Sync:** Upon reconnection, the client immediately requests a `SyncState` payload to ensure it didn't miss any votes while offline.
