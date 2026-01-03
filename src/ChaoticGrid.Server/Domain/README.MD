# Domain Architecture

This directory contains the **pure business logic** of Chaotic Grid. It is organized by **Aggregates** to ensure transactional consistency.

## Aggregates

### 1. Board (`BoardAggregate`)
**Root Entity:** `Board`
- Represents the definition of a game space.
- **Responsibilities:**
    - Managing the lifecycle (`Draft` -> `Active` -> `Archived`).
    - Storing the "Pool" of available tiles (and their approval status).
    - Managing configuration (Voting thresholds, Admins).
- **Invariants:**
    - A game cannot start without at least X approved tiles.
    - Configuration cannot be changed once the game is Active.

### 2. Match (`GameAggregate`)
**Root Entity:** `Match`
- Represents the *live instance* of a game currently being played.
- **Responsibilities:**
    - Tracking the global list of `ConfirmedEventIds` (The "Truth").
    - Managing active `Votes` (The "Process").
    - Handling timeouts (e.g., voting window expiration).
- **Invariants:**
    - A tile cannot be voted on if it is already confirmed.
    - A player cannot vote twice on the same event.

### 3. Player (`PlayerAggregate`)
**Root Entity:** `Player`
- Represents a user's participation in a specific board.
- **Responsibilities:**
    - Storing the user's specific **5x5 Grid Layout** (which is randomized per player).
    - Managing Identity (Value Object derived from JWT).
    - Tracking "Strikes" or "Silenced" status (if they submitted bad data).

## Common Value Objects
- **`PermissionSet`:** A calculated object describing exactly what a user can do at this moment.
- **`BingoPattern`:** The logic defining valid win conditions (Lines, Diagonals).