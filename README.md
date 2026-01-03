# Chaotic Grid

**Chaotic Grid** is a collaborative, real-time "Social Bingo" platform. It differs from traditional bingo in that tiles represent *predicted social situations* rather than random numbers. The game state is driven entirely by **user consensus**, meaning players must agree that an event occurred for it to become reality.

## Core Philosophy: "Fail Forward"
The system is designed around the narrative concept of "failing forward."
- **In Game:** If a player tries to claim an event happened and the group votes "No," the game doesn't stop. The player is penalized (silenced), the tile is marked as "Hot" (suspicious), and play continues with heightened stakes.
- **In Tech:** The system prioritizes continuity. If a client disconnects, they can rejoin seamlessly. "Winning" does not end the session; it merely announces a victor while the social experiment continues.

## System Architecture Overview

### 1. The Server (`src/ChaoticGrid.Server`)
**Role:** The Brain & The Truth.
- Responsible for all business logic, rule enforcement, and state management.
- Uses **Domain-Driven Design (DDD)** to model the complexity of social voting and game states.
- pushes "State Snapshots" to clients. It does not send raw data; it sends *instructions* on what the UI should look like.

### 2. The Client (`src/chaotic-grid-client`)
**Role:** The Dumb Terminal.
- A "Thin Client" that strictly renders what the server tells it to.
- Contains **zero business logic**. It does not calculate if a Bingo happened; it asks the server if a Bingo happened.
- Designed for resilience: if the internet cuts out, it attempts to reconnect and requests a full state sync.

## Key Features
- **Privacy First:** No usernames/passwords. Authentication is via "Magic Links" and JWTs. Data retention is minimal.
- **Social Consensus:** A real-time voting system ("Did Bob spill his drink?") where the group decides the truth.
- **Unified Lifecycle:** Boards transition from "Draft" (creation) to "Active" (play) without changing URLs or forcing players to reload.
- **Material Design:** A clean, accessible, mobile-first interface.