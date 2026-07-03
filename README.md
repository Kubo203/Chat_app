# Chat App — real-time chat (WPF + ASP.NET + MySQL)

A simple real-time chat application with rooms/channels. Messages are pushed live
over **raw WebSockets** and persisted in **MySQL**; a small **REST API** serves the
room list and message history. The desktop client is a **WPF** app.

## Architecture

```
WINDOWS PC                              MAC (or any host)
-----------                             -----------------
WPF client (C#, MVVM)                   ASP.NET Core backend
  - login + room list  ── REST ──►        GET/POST /api/rooms
  - chat view          ── WS ────►        GET /api/rooms/{id}/messages
  (run 2 windows to                       /ws?room={id}&user={name}
   demo live chat)                             │  Entity Framework Core
                                               ▼
                                          MySQL 8 (Docker)
```

- **REST** → list/create rooms, load recent history.
- **WebSocket** → live message push. The server keeps an in-memory registry of
  `roomId → connected sockets`; each incoming message is saved to MySQL and
  broadcast to everyone in that room.

## Tech stack

| Part | Tech |
|---|---|
| Desktop client | WPF (`.NET 9`, `net9.0-windows`), MVVM via `CommunityToolkit.Mvvm` |
| Backend | ASP.NET Core Web API (`.NET 10`) |
| Real-time | Raw WebSockets (`System.Net.WebSockets`) |
| Data access | Entity Framework Core **9** + `Pomelo.EntityFrameworkCore.MySql` 9.0.0 |
| Database | MySQL 8.4 (Docker) |

> Note: EF Core packages are pinned to the **9.0.x** line because the Pomelo MySQL
> provider does not yet ship an EF Core 10 build. A `net10.0` app references them
> without issue.

## Repository layout

```
.
├── docker-compose.yml     # MySQL 8.4 container
├── server/                # ASP.NET Core backend
└── client/                # WPF desktop client (built on Windows)
```

## Prerequisites

- **Docker** (for MySQL)
- **.NET 10 SDK** (backend)
- **Visual Studio 2022** with the *".NET desktop development"* workload (WPF client;
  Windows only)

## Running it

### 1. Start the database
```bash
docker compose up -d
```
Creates a `chatapp` database on `localhost:3306` (user `chat` / password `chatpass`).

### 2. Run the backend
```bash
cd server
dotnet ef database update          # first time only: creates the tables
dotnet run --urls "http://0.0.0.0:5173"
```
`0.0.0.0` makes the API reachable from other machines on the LAN (not just
`localhost`). Note the host's LAN IP (macOS: `ipconfig getifaddr en0`), e.g.
`192.168.0.25`.

Quick check: `curl http://localhost:5173/api/rooms` should return `[]` (or your rooms).

### 3. Run the client (Windows)
1. Open `client/ChatClient` in Visual Studio 2022.
2. In the app's **Server** field, enter the backend address, e.g.
   `http://192.168.0.25:5173` (use the backend host's LAN IP).
3. Enter a username, **Load Rooms**, create/pick a room, and chat.
4. To see real-time in action, run **two** client windows (same room) and watch
   messages appear instantly in both.

> Both machines must be on the same network. If the backend host has a firewall,
> allow incoming connections on port `5173`. If the LAN blocks it, you can run the
> backend + MySQL on the Windows PC too and use `http://localhost:5173`.

## Notes on secrets

The database credentials in `docker-compose.yml` and `server/appsettings.json` are
committed for easy local setup / grading. In a production project these would be kept
out of source control (environment variables / user-secrets).
