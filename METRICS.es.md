# Métricas (tamaño, complejidad, calidad)

Este repositorio incluye un conjunto pequeño y repetible de métricas para apoyar la memoria del proyecto (TFM).

English version: `METRICS.md`

## Qué medimos

### Tamaño

- **Ficheros trackeados**: número de ficheros versionados en git.
- **Líneas de código (LOC)**: total de líneas en ficheros trackeados con extensiones seleccionadas (`.cs`, `.csproj`, `.sln`, `.json`, `.yml/.yaml`, `.js/.ts/.tsx`, `.md`).

### Complejidad (proxies prácticos)

En lugar de tooling de pago, usamos indicadores prácticos que correlacionan bien con la complejidad:

- **Número de proyectos/servicios** (microservicios + AppHost).
- **Ficheros más grandes** (hotspots que suelen concentrar lógica y complejidad).
- **Media de líneas por fichero** (proxy aproximado de cuán dispersa/compacta está la lógica).

> Si necesitas complejidad ciclomática o maintainability index “formal”, integra una plataforma de análisis estático (p. ej. SonarQube/SonarCloud). Este enfoque ligero está pensado para poder ejecutarse fácilmente en local.

### Calidad

- **Tests automáticos**: resultado de `dotnet test`.
- **Cobertura**: reporte Cobertura usando `coverlet.collector`.
- (Opcional) **Warnings de compilación**: número de warnings en `dotnet build -c Release`.

## Cómo generar las métricas

Desde PowerShell en la raíz del repo:

- Ejecutar tests + cobertura:
  - `dotnet test CarLine.Tests/CarLine.Tests.csproj -c Release --collect:"XPlat Code Coverage"`

- Generar el informe de métricas:
  - `powershell -ExecutionPolicy Bypass -File scripts/metrics.ps1 | Out-File -Encoding utf8 metrics-report.md`

El script imprime:
- tamaño (ficheros, LOC, fichero más grande)
- proxies de complejidad (proyectos, top 5 ficheros más grandes)
- calidad (porcentajes de cobertura si existe el XML)

## Notas

- La cobertura se guarda en `CarLine.Tests/TestResults/**/coverage.cobertura.xml`.
- El repo usa `.NET 10` (`net10.0`), asegúrate de usar un SDK compatible.
