# CarLineProject

English / Español: see `README.es.md` for the Spanish version.

CarLine is an end-to-end solution for car listings ingestion, data cleanup, search, price prediction, and subscription alerts.

It is built as a .NET microservices architecture orchestrated with **.NET Aspire** (AppHost), using:
- **MongoDB** for raw/crawled data
- **Elasticsearch** for search + filters (facets)
- **Azure Blob Storage (Azurite)** for cleaned datasets and ML models
- **ML.NET (LightGBM)** for training and inference
- **SQL Server** for subscriptions
- **React + Vite** for the UI

## Architecture (high level)

Main data flow:

1. **ExternalCarSellerStub** simulates an external data source.
2. **Crawler** ingests listings (CSV/JSON) into MongoDB.
3. **DataCleanUp** cleans/normalizes data, indexes it into Elasticsearch, and uploads “cleaned” CSV files to Blob.
4. **TrainingFunction** (Azure Function) is triggered by blobs under “cleaned/*.csv”, trains a model, and uploads it to Blob.
5. **MLInterferenceService** loads the model from Blob and exposes prediction endpoints.
6. **API** exposes search (Elasticsearch) + price estimation (ML) + a subscription gateway.
7. **SubscriptionService** manages subscriptions (SQL) and processes notifications (worker + SMTP).
8. **Web** consumes `/api/*` via Vite proxy.

## Main projects

- CarLineProject: .NET Aspire AppHost (starts services and dependencies).
- CarLine.API: public API (`/api/*`) for search, prediction and subscriptions.
- CarLine.Web/car-line-web: React + Vite UI (proxy to the API).
- CarLine.Crawler: ingestion into MongoDB (`/api/ingestion/*`).
- CarLine.DataCleanUp: cleanup + indexing + blob export (`/api/cleanup/run`).
- CarLine.TrainingFunction: automatic training via BlobTrigger.
- CarLine.MLInterferenceService: ML inference (`/api/carprediction/*`).
- CarLine.SubscriptionService: subscriptions CRUD + manual processing trigger (`/api/subscriptions*`).
- CarLine.PriceClassificationService: background classification worker.

## Requirements

- **.NET SDK 10** (TargetFramework `net10.0`) and the Aspire packages used by the AppHost.
- **Docker Desktop** (needed for MongoDB, SQL Server, Elasticsearch and Azurite when running the AppHost).
- **Node.js** (for the Vite UI).

### Why .NET Aspire?

This project uses **.NET Aspire** as a *development orchestrator*.

In practice, the AppHost (project `CarLineProject`) handles:
- starting all microservices in the correct order,
- starting infrastructure dependencies and wiring environment variables,
- defining service-to-service references (e.g. Web proxying to the API),
- enabling a “one command” local run for the whole solution.

### Why Docker is required?

When you run the AppHost, the infrastructure dependencies run as **containers** (via Docker), for example:
- MongoDB
- SQL Server
- Elasticsearch
- Azurite (Azure Blob Storage emulator)

Without Docker, those components will not exist and dependent services will fail at startup or at runtime.

> Note: the AppHost uses persistent Docker volumes (e.g. `carline-mongodb-data`, `carline-elasticsearch-data`, `carline-azurite-data`) so data/models survive restarts.

#### Windows

On Windows, **Docker Desktop + WSL2** is recommended.

### Docker-free fallback (not recommended)

If you cannot use Docker, you must manually install/configure MongoDB, Elasticsearch, SQL Server and an Azurite/Blob-compatible service and then point each service’s `appsettings*.json` `ConnectionStrings`/endpoints to those instances.

## Run locally (recommended: Aspire/AppHost)

From the repo root:

- `dotnet run --project CarLineProject/CarLineProject.csproj`

This starts:
- MongoDB + DB `carsnosql`
- SQL Server + DB `subscriptionsdb` (mapped to host port `54040`)
- Elasticsearch
- Azurite + blob container for models/datasets
- all .NET services
- the UI (Vite) with automatic npm package installation

## Key endpoints (quick)

Public API:
- `GET /api/CarsSearch/search` (search + filters)
- `POST /api/CarPricePrediction/estimate` (price estimate)
- `POST /api/CarSubscription` / `GET /api/CarSubscription?email=...` / `DELETE /api/CarSubscription/{id}`

Internal services:
- Crawler: `POST /api/Ingestion/upload` (CSV) and `POST /api/Ingestion/upload-json` (JSON)
- Cleanup: `POST /api/Cleanup/run`
- ML inference: `POST /api/CarPrediction/predict` and `POST /api/CarPrediction/predict/batch`
- Subscriptions (direct): `POST/GET/DELETE /api/subscriptions` + `POST /api/subscriptions-processing/run`

## Tests

- `dotnet test CarLine.Tests/CarLine.Tests.csproj`

## Releases

This repo uses tag-based versioning (`vX.Y.Z`). See the changelog in `CHANGELOG.md` (EN) / `CHANGELOG.es.md` (ES).

To publish a release on GitHub:
- create an annotated tag (`git tag -a v1.0.0 -m "v1.0.0"`)
- push the tag (`git push origin v1.0.0`)
- create a GitHub Release from the UI (or with `gh release create`)
