# Metrics report

Generated: 2026-01-19 23:43:47
Commit: d64c8f0

This report is generated from tracked source files and test/build artifacts.
It is intended as evidence for project size, complexity proxies and quality.

## Environment
- OS: Microsoft Windows NT 10.0.26100.0
- PowerShell: 5.1.26100.6899
- dotnet SDK: 10.0.101

## Scope & exclusions
- Source of truth: 'git ls-files' (tracked files only)
- Excluded from LOC metrics: lockfiles (package-lock/yarn/pnpm), TestResults, generated C# (*.g.cs/*.designer.cs)

## Methodology (summary)
- LOC: raw line count (including blanks) for selected tracked file types
- Complexity: proxies (projects/services, controllers/endpoints, largest files)
- Quality: tests + coverage (Cobertura) + optional build warnings

## Size
- Tracked files: 180
- Files (selected extensions): 159
- Total lines (selected extensions): 9374
- Avg lines per file (selected extensions): 58.96
- Largest file: CarLine.Web/car-line-web/src/components/SubscriptionModal.tsx (454 lines)
- LOC by extension (selected):
  - .cs: 85 files, 5648 lines
  - .tsx: 9 files, 1755 lines
  - .md: 9 files, 483 lines
  - .json: 31 files, 474 lines
  - .ts: 8 files, 348 lines
  - .csproj: 11 files, 250 lines
  - .js: 5 files, 245 lines
  - .sln: 1 files, 171 lines
- LOC by top-level folder (selected extensions):
  - CarLine.Web: 2569 lines
  - CarLine.DataCleanUp: 1042 lines
  - CarLine.SubscriptionService: 838 lines
  - CarLine.Crawler: 742 lines
  - CarLine.Common: 741 lines
  - CarLine.API: 722 lines
  - CarLine.PriceClassificationService: 615 lines
  - CarLine.Tests: 524 lines
  - CarLine.MLInterferenceService: 310 lines
  - CarLine.ExternalCarSellerStub: 304 lines

## Complexity (proxies)
- Projects (.csproj): 11
- Services/apps (from solution layout): 9
- Controller files: 9
- Background workers (Worker.cs): 4
- HTTP endpoints (approx., attribute count in controllers): 18
- Top 5 largest tracked files (selected extensions):
  - CarLine.Web/car-line-web/src/components/SubscriptionModal.tsx: 454
  - CarLine.Web/car-line-web/src/components/PriceEstimator.tsx: 391
  - CarLine.Crawler/Controllers/IngestionController.cs: 355
  - CarLine.Web/car-line-web/src/components/FiltersForm.tsx: 351
  - CarLine.DataCleanUp/Services/DataCleanupService.cs: 315

## Dependencies
- PackageReference entries (all .csproj): 62
- Unique packages (all .csproj): 39
- Sample packages: Aspire.Azure.Storage.Blobs, Aspire.Hosting.AppHost, Aspire.Hosting.Azure.Functions, Aspire.Hosting.Azure.Storage, Aspire.Hosting.Elasticsearch, Aspire.Hosting.MongoDB, Aspire.Hosting.NodeJs, Aspire.Hosting.SqlServer, Azure.Storage.Blobs, CommunityToolkit.Aspire.Hosting.NodeJS.Extensions

## Quality
- Test files (.cs under CarLine.Tests): 3
- Build: running 'dotnet build -c Release' (this may take a moment)
- Build warnings (approx.): 8
- Build errors (approx.): 0
- Build succeeded: True
- Warning codes: NU1510
- Tests: running 'dotnet test -c Release' (this may take a moment)
- Coverage (Cobertura): line-rate=16.35% branch-rate=11.11%
- Coverage details: lines 216/1321, branches 76/684
- Coverage file: CarLine.Tests/TestResults/e6aacbe4-1c0c-4b92-a0ac-9f603eb004e4/coverage.cobertura.xml

## Limitations
- LOC is a size indicator, not a direct quality measure.
- Endpoint count is approximate (attribute-based count in controller files).
- Coverage % depends on which assemblies are included by the test suite; low coverage indicates improvement areas.
- Build warnings: run 'dotnet build -c Release' and review warnings output (optional metric)
