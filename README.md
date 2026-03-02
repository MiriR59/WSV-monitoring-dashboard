# WSV - Monitoring Dashboard
## Overview

Web Supervisor is a backend-focused monitoring dashboard that simulates multiple data sources producing time-based readings.  
The system processes these readings through an event-driven pipeline, temporarily stores recent data in memory, and persists historical data into a database for long-term access.

The primary goal of this project was to design and implement a backend architecture that models real-world data-flow systems, focusing on service lifecycles, data consistency, authorization, and separation of concerns.

---

## Key Implementation Decisions

- **Dynamic buffer with overflow channels** – The in-memory buffer
  (built on .NET Channels) automatically expands by adding overflow
  channels when load exceeds 60% capacity, and shrinks them back when
  pressure drops. This prevents data loss under burst traffic without
  pre-allocating memory for peak load.

- **Two-layer read strategy** – Recent data is served from a TTL-expiring
  in-memory cache; historical queries hit the database. When cache and DB
  results overlap, deduplication is handled via a timestamp-keyed Dictionary
  in a single pass. Historical queries automatically switch between raw and
  time-bucket aggregated SQL depending on result count.

- **Service lifetime design** – Cache and buffer are singletons (shared state
  across the app); ReadingService is scoped (one per request). Background
  services use IServiceScopeFactory to resolve scoped DbContext safely.

- **Unit tested with xUnit and Moq** – test cover the core read logic:
  DB-only, cache-only, cache+DB overlap with deduplication, time-boundary
  filtering, and all three lag states (NoLiveData / DbEmpty / Ok).
---

## Tech Stack

**Backend**
- ASP.NET Core Web API
- Entity Framework Core
- Background services (IHostedService)
- PostgreSQL
- xUnit
- Moq

**Frontend**
- Angular

**Other**
- JWT authentication
- Role-based authorization

---

## Data Flow (High Level)

1. Hosted service generates a reading for a given source.
2. Reading is pushed into an in-memory buffer / pipeline.
3. Recent readings are stored in short-term cache (for near real-time endpoints).
4. Readings are persisted into the database (for historical endpoints).
5. API exposes:
   - near real-time data (cache-based)
   - historical data (DB-based)
   - management endpoints (protected)

---

## Repository Structure

- `WSV.Api/` – ASP.NET Core backend (API + background services)
- `WSV.App/` – Angular frontend
- `WSV.sln` – .NET solution

---

## Demo Users & Authorization

The application demonstrates role-based authorization with multiple user roles.

On startup, demo users are seeded automatically.

### Public Access (No Login)
- Can access public endpoints
- Sees only the first data source

### Viewer
- Username: `viewer`
- Password: `Viewer123`
- Can view all data sources
- Cannot modify system state

### Operator
- Username: `operator`
- Password: `Operator123`
- Can view all data sources
- Can enable / disable sources

### Admin
- Username: `admin`
- Password: `Admin123`
- Full access to all protected endpoints
- Intended to manage advanced administrative operations (future extension)

---

## Getting Started

This setup demonstrates:
- Public vs protected endpoints
- JWT-based authentication
- Role-based authorization policies

### Prerequisites
- .NET 8 SDK
- Node.js (LTS recommended)
- npm (bundled with Node.js)

### Restore .NET local tools
```bash
dotnet tool restore
```

### Run Backend (API)

```bash
cd WSV.Api
dotnet restore
dotnet run
```

### Run Frontend (Angular)
```bash
cd WSV.App
npm install
npm start
```
