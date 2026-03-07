# WSV - Monitoring Dashboard
## Overview

Web Supervisor is a backend-focused monitoring dashboard that simulates multiple data sources producing time-based readings. The system processes these readings through an event-driven pipeline, temporarily stores recent data in cache, buffers the data and persists historical data into a database for long-term access.

The primary goal of this project was to design and implement a backend architecture that models real-world data-flow systems, focusing on service lifecycles, data consistency, authorization, and separation of concerns.

---

## Architecture Overview
```
┌─────────────────────┐
│   Angular Frontend  │
│    (Port 4200)      │
└──────────┬──────────┘
           │ HTTP/REST
┌──────────▼─────────────────────────────────┐
│              ASP.NET Core API (Port 8080)  │
│                                            │
│  ┌────────────┐         ┌─────────────┐    │
│  │ Controllers│────────>│  Services   │    │
│  │   (thin)   │         │  (business) │    │
│  └────────────┘         └──────┬──────┘    │
│                                │           │
│                                │ reads     │
│                          ┌─────▼─────┐     │
│                          │   Cache   │     │
│                          │ (60s TTL) │     │
│                          └─────▲─────┘     │
│                                │           │
│  ┌──────────────────┐          │           │
│  │ GeneratorService │          │ updates   │
│  │     (Hosted)     │          │           │
│  └────────┬─────────┘          │           │
│           │ produces           │           │
│           │                    │           │
│  ┌────────▼────────────────┐   │           │
│  │  DynamicBufferService   │───┘           │
│  │  (Singleton, Channel)   │               │
│  │  • Auto-expand          │               │
│  │  • Auto-shrink          │               │
│  └────────┬────────────────┘               │
│           │ consumes                       │
│           │                                │
│  ┌────────▼─────────┐                      │
│  │  DbWriterService │                      │
│  │     (Hosted)     │                      │
│  │ • Batch writes   │                      │
│  │ • Slow/Fast mode │                      │
│  └────────┬─────────┘                      │
└───────────┼────────────────────────────────┘
            │ writes
       ┌────▼────────┐
       │ PostgreSQL  │◄────reads────(Services)
       └─────────────┘
```

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
---

## Testing

Unit tests are written with xUnit and Moq, covering:
- Raw history strategy (DB-only, cache-only, overlap deduplication, time filtering)
- Strategy selection (raw vs. aggregate switching logic)
- Database lag states (NoLiveData / DbEmpty / Ok)
- Source cache behavior (empty/filled, copy-not-reference verification, updating)

---

## Tech Stack

**Backend**
- ASP.NET Core Web API
- Entity Framework Core
- Background services (IHostedService)
- PostgreSQL
- xUnit + Moq

**Frontend**
- Angular

**Other**
- JWT authentication
- Role-based authorization
- Docker + Docker Compose

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

### Running with Docker

1. Install Docker
2. Clone the repo
3. Run `docker compose up --build`
4. Frontend: http://localhost:4200
5. API / Swagger: http://localhost:8080/swagger
