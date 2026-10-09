# Naturalist Atlas (自然地图志)

> A nature exploration & observation map, powered by open biodiversity data from **iNaturalist** and **GBIF**.

English | [简体中文](Readme_cn.md)

**Naturalist Atlas** visualizes real-world species observations on an interactive map. It is a **read-only** exploration product by design — it does not accept observation uploads. The MVP covers **Beijing**, and the scope will gradually expand to more cities and even beyond biology (terrain, geology, astronomy and other natural resources).

---

## Features

- 🗺 **Map exploration** — MapLibre GL map with clustered observation points, city switching, time slider (month / ten-day periods), filters for iconic taxon, quality grade and photo-only
- 🐦 **Species** — species rank (most recorded / most observers / rarest), species profile with ancestor chain and similar-species discovery
- 🌸 **Phenology** — ten-day and monthly emergence statistics, phenology matrix (top species × 12 months) with peak-month sorting and native PNG export
- 🏙 **City overview** — 12-month record trends, iconic-taxon distribution, rising stars of the month
- 🎯 **Collection gaps** — per-city "wanted taxa" list for completing a city's collection book
- 🚩 **Content safety** — public report submissions with a moderation workflow (hide / restore observations, close tickets)
- 🛠 **Management site** — countries / provinces / cities, taxa, observations, reports, sync tasks & logs, and an operations dashboard (Chart.js)
- 🔄 **Dual-source collector** — scheduled jobs syncing from iNaturalist (API v2) and GBIF (Occurrence API) with watermark-based incremental sync, time-slice backfill, deletion reconciliation, rate limiting and retry/backoff

## Tech Stack

| Layer | Technologies |
| --- | --- |
| Backend | .NET 8, custom **Rampage** framework (MVC / DAL / scheduler), PostgreSQL |
| Admin site | ASP.NET Core Razor (build-time compiled), Bootstrap 5, jQuery, Chart.js |
| Frontend | Vue 3 + Vite + TypeScript, MapLibre GL, supercluster, Pinia, TanStack Query, vue-i18n, Naive UI, ECharts, Vitest |
| Data sources | [iNaturalist API v2](https://api.inaturalist.org/v2/docs/), [GBIF Occurrence API](https://www.gbif.org/developer/summary) |

## Project Structure

```
Rampage.NaturalistAtlas/
├── Rampage.NaturalistAtlas.Net/                  # .NET 8 backend (see its Readme.md)
│   ├── Rampage.NaturalistAtlas.Model             # Po / Dto / Enums / Filters
│   ├── Rampage.NaturalistAtlas.Common            # Project-level shared logic
│   ├── Rampage.NaturalistAtlas.DAL               # Data access layer (all SQL lives here)
│   ├── Rampage.NaturalistAtlas.BLL               # Business logic layer
│   ├── Rampage.NaturalistAtlas.WebApi            # Public + admin JSON API
│   ├── Rampage.NaturalistAtlas.ManagementSite    # Management site (Razor, port 5213)
│   ├── Rampage.NaturalistAtlas.Collector         # Data collection worker (iNat / GBIF ETL)
│   ├── Rampage.NaturalistAtlas.Tests             # Unit tests (xUnit)
│   └── Rampage.NaturalistAtlas.sln
├── Rampage.NaturalistAtlas.Web/                  # Vue 3 SPA frontend (dev port 5174)
└── documents/                                    # Product & design documents, table designs
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- PostgreSQL 14+

### 1. Database

Create two databases (business + management) and run the SQL scripts in
[`documents/DatabaseTablesDesign/`](documents/DatabaseTablesDesign/Readme.md):

- `NaturalistAtlas.TablesDesign` — business schema: country / province / city / taxon / observation / sync_cursor / sync_log / report / emergence_stat / emergence_history / city_wanted_taxon, plus geo seed data (249 countries, 357 provinces, 362 cities)
- `NaturalistAtlas.Management.TablesDesign` — management schema: admins, menus, permissions, settings, operation logs

### 2. Backend

Each site reads `config/app.json` (database connection, AES key, logging). Configure the connections, then:

```bash
# Public JSON API (http://localhost:5214)
dotnet run --project Rampage.NaturalistAtlas.Net/Rampage.NaturalistAtlas.WebApi

# Management site (http://localhost:5213)
dotnet run --project Rampage.NaturalistAtlas.Net/Rampage.NaturalistAtlas.ManagementSite
```

Run the test suite:

```bash
dotnet test Rampage.NaturalistAtlas.Net/Rampage.NaturalistAtlas.Tests
```

### 3. Frontend

```bash
cd Rampage.NaturalistAtlas.Web
npm install
npm run dev        # http://localhost:5174
npm run build      # type-check + production bundle in dist/
npm test           # Vitest
```

### 4. Collector

```bash
dotnet run --project Rampage.NaturalistAtlas.Net/Rampage.NaturalistAtlas.Collector
```

Jobs are scheduled per city/source with watermarks stored in `sync_cursor`: incremental sync, time-slice backfill, taxon profile refresh, deletion reconciliation, emergence histogram aggregation and wanted-taxa sync. A one-shot wanted-taxa run is available via `--run-wanted`.

## Public API (JSON)

| Endpoint | Description |
| --- | --- |
| `GET /api/geo/search` · `GET /api/geo/cities` | Place search & city list |
| `GET /api/observation/points` · `GET /api/observation/list` | Map points, paged observation cards |
| `GET /api/species/rank` · `GET /api/species/{id}` · `GET /api/species/{id}/similar` · `GET /api/species/{id}/emergence` | Species rank, detail, similar species, emergence series |
| `GET /api/taxon/{id}/ancestors` | Taxon ancestry breadcrumb |
| `GET /api/phenology/matrix` | Phenology matrix (top species × 12 months) |
| `GET /api/stats/overview` | City overview statistics |
| `GET /api/gaps` | City collection gaps (wanted taxa) |
| `POST /api/report` | Submit a content report |

## Code Style

C# conventions are enforced via [.editorconfig](Rampage.NaturalistAtlas.Net/.editorconfig): PascalCase types/members, camelCase locals, `UPPER_SNAKE_CASE` constants, 4-space indentation, Allman braces (always braced), alphabetically sorted usings, single statement per line, and the Service/DAO naming rules (`Get…` / `…List` / `…Count` / `Save…` / `Remove/Delete` / `Update…`). SQL is allowed only in the DAL layer.

## Documentation (Chinese)

- Product document: [documents/ProductDocuments/NaturalistAtlas.ProductDocument.md](documents/ProductDocuments/NaturalistAtlas.ProductDocument.md)
- Frontend design document: [documents/ProductDocuments/NaturalistAtlas.FrontendDesignDocument.md](documents/ProductDocuments/NaturalistAtlas.FrontendDesignDocument.md)
- Table designs: [documents/DatabaseTablesDesign/](documents/DatabaseTablesDesign/)
- Backend readme: [Rampage.NaturalistAtlas.Net/Readme.md](Rampage.NaturalistAtlas.Net/Readme.md)

## Author

**Nicholas Chow (XTiger1986)** · [1986z.com](https://1986z.com)

© 2026. All rights reserved.
