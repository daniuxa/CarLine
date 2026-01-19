# Changelog

Format loosely based on *Keep a Changelog*.

## v1.0.0 — 2026-01-19

### Added
- AppHost (.NET Aspire) to orchestrate MongoDB, SQL Server, Elasticsearch and Azurite.
- Ingestion → cleanup → training → inference → API → Web pipeline.
- Search, price prediction and subscription endpoints.

### Notes
- Main TargetFramework: `net10.0`.
- UI: React + Vite with `/api` proxy to `carlineapi` (Aspire).
