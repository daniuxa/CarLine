# CarLineProject

CarLine es una solución *end‑to‑end* para ingesta, limpieza, búsqueda, predicción de precio y suscripción a anuncios de coches.

Está implementada como una arquitectura de microservicios .NET orquestada con **.NET Aspire** (AppHost), usando:
- **MongoDB** para datos crudos/crawleados
- **Elasticsearch** para búsquedas y filtros (facetas)
- **Azure Blob Storage (Azurite)** para datasets limpios y modelos
- **ML.NET (LightGBM)** para entrenamiento e inferencia
- **SQL Server** para suscripciones
- **React + Vite** para la UI

## Arquitectura (resumen)

Flujo principal de datos:

1. **ExternalCarSellerStub** simula una fuente externa.
2. **Crawler** ingesta listados (CSV/JSON) hacia MongoDB.
3. **DataCleanUp** limpia/normaliza datos, indexa en Elasticsearch y sube CSV “cleaned” a Blob.
4. **TrainingFunction** (Azure Function) se dispara con blobs “cleaned/*.csv”, entrena y sube un modelo a Blob.
5. **MLInterferenceService** carga el modelo desde Blob y expone endpoints de predicción.
6. **API** expone endpoints de búsqueda (Elasticsearch) + estimación de precio (ML) + gateway de suscripciones.
7. **SubscriptionService** gestiona suscripciones (SQL) y procesa envíos (worker + SMTP).
8. **Web** consume `/api/*` vía proxy de Vite.

## Proyectos principales

- CarLineProject: AppHost (.NET Aspire) que levanta servicios y dependencias.
- CarLine.API: API pública (`/api/*`) para búsqueda, predicción y suscripciones.
- CarLine.Web/car-line-web: UI React + Vite (proxy a la API).
- CarLine.Crawler: ingesta hacia MongoDB (`/api/ingestion/*`).
- CarLine.DataCleanUp: limpieza + indexado + export a Blob (`/api/cleanup/run`).
- CarLine.TrainingFunction: entrenamiento automático por BlobTrigger.
- CarLine.MLInterferenceService: inferencia ML (`/api/carprediction/*`).
- CarLine.SubscriptionService: CRUD de suscripciones + procesamiento manual (`/api/subscriptions*`).
- CarLine.PriceClassificationService: worker de clasificación (background service).

## Requisitos

- **.NET SDK 10** (TargetFramework `net10.0`) y workload/paquetes de Aspire usados en el AppHost.
- **Docker Desktop** (para MongoDB, SQL Server, Elasticsearch y Azurite cuando se ejecuta el AppHost).
- **Node.js** (para la UI con Vite).

> Nota: el AppHost usa volúmenes Docker persistentes (por ejemplo `carline-mongodb-data`, `carline-elasticsearch-data`).

## Ejecutar en local (recomendado: Aspire/AppHost)

Desde la raíz del repo:

- `dotnet run --project CarLineProject/CarLineProject.csproj`

Esto levanta:
- MongoDB + DB `carsnosql`
- SQL Server + DB `subscriptionsdb` (mapeado a host port `54040`)
- Elasticsearch
- Azurite + container de blobs para modelos/datasets
- Todos los servicios .NET
- La UI (Vite) con instalación automática de paquetes npm

## Endpoints clave (rápido)

API pública:
- `GET /api/CarsSearch/search` (búsqueda + filtros)
- `POST /api/CarPricePrediction/estimate` (estimación de precio)
- `POST /api/CarSubscription` / `GET /api/CarSubscription?email=...` / `DELETE /api/CarSubscription/{id}`

Servicios internos:
- Crawler: `POST /api/Ingestion/upload` (CSV) y `POST /api/Ingestion/upload-json` (JSON)
- Cleanup: `POST /api/Cleanup/run`
- Inferencia ML: `POST /api/CarPrediction/predict` y `POST /api/CarPrediction/predict/batch`
- Suscripciones (directo): `POST/GET/DELETE /api/subscriptions` + `POST /api/subscriptions-processing/run`

## Tests

- `dotnet test CarLine.Tests/CarLine.Tests.csproj`

## Releases

Este repo usa versionado con tags (`vX.Y.Z`). Ver CHANGELOG en `CHANGELOG.md`.

Para publicar una release en GitHub normalmente se hace:
- crear tag (`git tag -a v1.0.0 -m "v1.0.0"`)
- push del tag (`git push origin v1.0.0`)
- crear GitHub Release desde la UI (o con `gh release create`)
