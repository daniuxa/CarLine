# Metrics (size, complexity, quality)

This repository includes a small, repeatable metrics setup to support the project report (TFM).

Spanish version: `METRICS.es.md`

## What we measure

### Size

- **Tracked files**: count of files tracked by git.
- **Lines of code (LOC)**: total lines across tracked files with selected extensions (`.cs`, `.csproj`, `.sln`, `.json`, `.yml/.yaml`, `.js/.ts/.tsx`, `.md`).

### Complexity (pragmatic proxies)

Instead of paid tooling, we use practical indicators that correlate well with complexity:

- **Number of projects/services** (microservices + AppHost).
- **Largest files** (hotspots that tend to concentrate logic and complexity).
- **Average lines per file** (rough proxy for how spread/complex logic is).

> If you need formal cyclomatic complexity or maintainability index, integrate a static-analysis platform (e.g., SonarQube/SonarCloud). The lightweight approach here is meant to be easy to run locally.

### Quality

- **Automated tests**: `dotnet test` result.
- **Test coverage**: Cobertura report from `coverlet.collector`.
- (Optional) **Build warnings**: `dotnet build -c Release` warnings count.

## How to generate the metrics

From PowerShell at repo root:

- Run tests + coverage:
  - `dotnet test CarLine.Tests/CarLine.Tests.csproj -c Release --collect:"XPlat Code Coverage"`

- Generate the metrics report:
  - `powershell -ExecutionPolicy Bypass -File scripts/metrics.ps1 | Out-File -Encoding utf8 metrics-report.md`

The script prints:
- size numbers (files, LOC, largest file)
- complexity proxies (projects, top 5 largest files)
- quality numbers (coverage percentages if available)

## Notes

- Coverage output is stored under `CarLine.Tests/TestResults/**/coverage.cobertura.xml`.
- This repo targets `.NET 10` (`net10.0`), so make sure you use a compatible SDK.
