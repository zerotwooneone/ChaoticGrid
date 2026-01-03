# Chaotic Grid - Server Architecture

The server is built on **.NET 10** and acts as the authoritative source of truth for the game. It follows a strict **Clean Architecture** pattern to separate the Core Domain from external concerns (like the database or the web).

## Architectural Layers

### 1. Api (`/Api`)
**The Entry Point.**
- Contains **Minimal API** endpoints for HTTP requests (initial load, joining).
- Handles DTO (Data Transfer Object) mapping.
- **Responsibility:** Receive request -> Validate format -> Dispatch to Domain -> Return result.

### 2. Domain (`/Domain`)
**The Core.**
- Contains the business logic, Aggregates, and Entities.
- **Pure C#:** No dependencies on Entity Framework, SignalR, or HTTP contexts.
- See `/Domain/README.md` for specific aggregate breakdowns.

### 3. Infrastructure (`/Infrastructure`)
**The Plumbing.**
- **Persistence:** Implementation of Repositories using EF Core (SQLite).
- **Hubs:** SignalR Hubs that manage real-time WebSocket connections.
- **Responsibility:** The Hubs act as the "Presentation Layer" for real-time events, translating Domain Events (e.g., `TileConfirmed`) into Client Messages (e.g., `UpdateGrid`).

## The "Pit of Success" Pattern
The API is designed to make it impossible for the frontend to enter an invalid state.
- We do not send `UserRole: "Guest"`.
- We send `Permissions: { CanVote: false, CanSuggest: true }`.
  This decouples the frontend from backend role logic.