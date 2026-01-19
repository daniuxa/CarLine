# Changelog

El formato sigue una variante de *Keep a Changelog*.

## v1.0.0 — 2026-01-19

### Added
- AppHost (.NET Aspire) para orquestar MongoDB, SQL Server, Elasticsearch y Azurite.
- Pipeline de ingesta → limpieza → entrenamiento → inferencia → API → Web.
- Endpoints de búsqueda, predicción de precio y suscripciones.

### Notes
- TargetFramework principal: `net10.0`.
- UI: React + Vite con proxy `/api` hacia `carlineapi` (Aspire).
