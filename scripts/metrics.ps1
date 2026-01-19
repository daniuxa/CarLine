param(
  [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")),
  [switch]$RunBuild,
  [switch]$RunTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Section([string]$title) {
  "" | Write-Output
  "## $title" | Write-Output
}

function Get-TrackedFiles([string]$repo) {
  $files = git -C $repo ls-files
  if ($LASTEXITCODE -ne 0) { throw "git ls-files failed" }
  return $files
}

function Get-RepoRelativePath([string]$repo, [string]$fullPath) {
  $repoNorm = $repo.TrimEnd('\','/')
  $p = $fullPath
  if ($p.ToLowerInvariant().StartsWith($repoNorm.ToLowerInvariant())) {
    $p = $p.Substring($repoNorm.Length).TrimStart('\','/')
  }
  return $p.Replace('\','/')
}

function Is-ExcludedPath([string]$path) {
  $p = $path.Replace('\\', '/').ToLowerInvariant()

  # Generated / non-source artifacts that would skew metrics
  if ($p.EndsWith('/package-lock.json')) { return $true }
  if ($p.EndsWith('/yarn.lock')) { return $true }
  if ($p.EndsWith('/pnpm-lock.yaml')) { return $true }
  if ($p -like '*/testresults/*') { return $true }
  if ($p.EndsWith('.g.cs')) { return $true }
  if ($p.EndsWith('.designer.cs')) { return $true }

  return $false
}

function Get-LineCount([string]$path) {
  if (!(Test-Path $path)) { return 0 }
  return (Get-Content $path -Raw | Measure-Object -Line).Lines
}

$repo = (Resolve-Path $RepoRoot).Path
$runAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
$head = (git -C $repo rev-parse --short HEAD)

Write-Output "# Metrics report"
Write-Output ""
Write-Output "Generated: $runAt"
Write-Output "Commit: $head"

Write-Output ""
Write-Output "This report is generated from tracked source files and test/build artifacts."
Write-Output "It is intended as evidence for project size, complexity proxies and quality."

Write-Section "Environment"
$dotnetVersion = (& dotnet --version 2>$null)
Write-Output "- OS: $([System.Environment]::OSVersion.VersionString)"
Write-Output "- PowerShell: $($PSVersionTable.PSVersion)"
if ($dotnetVersion) {
  Write-Output "- dotnet SDK: $dotnetVersion"
}

$tracked = Get-TrackedFiles $repo

Write-Section "Scope & exclusions"
Write-Output "- Source of truth: 'git ls-files' (tracked files only)"
Write-Output "- Excluded from LOC metrics: lockfiles (package-lock/yarn/pnpm), TestResults, generated C# (*.g.cs/*.designer.cs)"

Write-Section "Methodology (summary)"
Write-Output "- LOC: raw line count (including blanks) for selected tracked file types"
Write-Output "- Complexity: proxies (projects/services, controllers/endpoints, largest files)"
Write-Output "- Quality: tests + coverage (Cobertura) + optional build warnings"

Write-Section "Size"
$trackedCount = ($tracked | Measure-Object).Count
Write-Output "- Tracked files: $trackedCount"

$extensions = @(
  ".cs", ".csproj", ".sln", ".json", ".yml", ".yaml", ".js", ".ts", ".tsx", ".md"
)
$filtered = $tracked | Where-Object {
  $ext = [System.IO.Path]::GetExtension($_)
  ($extensions -contains $ext) -and -not (Is-ExcludedPath $_)
}

$lineCounts = foreach ($f in $filtered) {
  $full = Join-Path $repo $f
  [pscustomobject]@{ Path = $f; Lines = (Get-LineCount $full) }
}

$totalLines = ($lineCounts | Measure-Object -Property Lines -Sum).Sum
$avgLines = [math]::Round(($lineCounts | Measure-Object -Property Lines -Average).Average, 2)
$maxFile = $lineCounts | Sort-Object Lines -Descending | Select-Object -First 1

Write-Output "- Files (selected extensions): $($lineCounts.Count)"
Write-Output "- Total lines (selected extensions): $totalLines"
Write-Output "- Avg lines per file (selected extensions): $avgLines"
Write-Output "- Largest file: $($maxFile.Path) ($($maxFile.Lines) lines)"

# Breakdown by extension
$byExt = $lineCounts | Group-Object { [System.IO.Path]::GetExtension($_.Path).ToLowerInvariant() } | ForEach-Object {
  [pscustomobject]@{ Ext = $_.Name; Files = $_.Count; Lines = (($_.Group | Measure-Object -Property Lines -Sum).Sum) }
} | Sort-Object Lines -Descending

Write-Output "- LOC by extension (selected):"
foreach ($row in $byExt) {
  Write-Output ("  - {0}: {1} files, {2} lines" -f $row.Ext, $row.Files, $row.Lines)
}

# Breakdown by top-level folder (roughly, per project)
$byTop = $lineCounts | ForEach-Object {
  $parts = $_.Path -split '[\\/]'
  $top = if ($parts.Length -gt 0) { $parts[0] } else { "(root)" }
  [pscustomobject]@{ Top = $top; Lines = $_.Lines }
} | Group-Object Top | ForEach-Object {
  [pscustomobject]@{ Top = $_.Name; Lines = (($_.Group | Measure-Object -Property Lines -Sum).Sum) }
} | Sort-Object Lines -Descending

Write-Output "- LOC by top-level folder (selected extensions):"
foreach ($row in ($byTop | Select-Object -First 10)) {
  Write-Output ("  - {0}: {1} lines" -f $row.Top, $row.Lines)
}

Write-Section "Complexity (proxies)"

$csprojCount = ($tracked | Where-Object { $_.EndsWith(".csproj") } | Measure-Object).Count
$serviceProjects = @(
  "CarLine.API",
  "CarLine.Crawler",
  "CarLine.DataCleanUp",
  "CarLine.ExternalCarSellerStub",
  "CarLine.MLInterferenceService",
  "CarLine.PriceClassificationService",
  "CarLine.SubscriptionService",
  "CarLine.TrainingFunction",
  "CarLineProject"
)

Write-Output "- Projects (.csproj): $csprojCount"
Write-Output "- Services/apps (from solution layout): $($serviceProjects.Count)"

# Controllers / Workers / endpoints (approx.)
$controllerFiles = @($tracked | Where-Object { $_ -match '/Controllers/.*\.cs$' -and -not (Is-ExcludedPath $_) })
$workerFiles = @($tracked | Where-Object { $_.EndsWith("Worker.cs") -and -not (Is-ExcludedPath $_) })

$endpointCount = 0
foreach ($f in $controllerFiles) {
  $full = Join-Path $repo $f
  if (!(Test-Path $full)) { continue }
  $txt = Get-Content -Raw $full
  $endpointCount += ([regex]::Matches($txt, '\[(HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)\b', "IgnoreCase")).Count
}

Write-Output "- Controller files: $($controllerFiles.Count)"
Write-Output "- Background workers (Worker.cs): $($workerFiles.Count)"
Write-Output "- HTTP endpoints (approx., attribute count in controllers): $endpointCount"

Write-Output "- Top 5 largest tracked files (selected extensions):"
$top5 = $lineCounts | Sort-Object Lines -Descending | Select-Object -First 5
foreach ($t in $top5) {
  Write-Output "  - $($t.Path): $($t.Lines)"
}

Write-Section "Dependencies"

$csprojFiles = $tracked | Where-Object { $_.EndsWith(".csproj") }
$packageRefCount = 0
$packageIds = New-Object System.Collections.Generic.List[string]
foreach ($p in $csprojFiles) {
  $full = Join-Path $repo $p
  if (!(Test-Path $full)) { continue }
  $xmlText = Get-Content -Raw $full
  $matches = [regex]::Matches($xmlText, '<PackageReference\s+Include="([^"]+)"', "IgnoreCase")
  $packageRefCount += $matches.Count
  foreach ($m in $matches) { $packageIds.Add($m.Groups[1].Value) }
}
$uniquePackages = $packageIds | Sort-Object -Unique
Write-Output "- PackageReference entries (all .csproj): $packageRefCount"
Write-Output "- Unique packages (all .csproj): $($uniquePackages.Count)"
Write-Output "- Sample packages: $([string]::Join(', ', ($uniquePackages | Select-Object -First 10)))"

Write-Section "Quality"

# Tests
$testFileCount = ($tracked | Where-Object { $_ -like "CarLine.Tests/*" -and $_.EndsWith(".cs") } | Measure-Object).Count
Write-Output "- Test files (.cs under CarLine.Tests): $testFileCount"

if ($RunBuild) {
  Write-Output "- Build: running 'dotnet build -c Release' (this may take a moment)"
  $solutionPath = Join-Path $repo "CarLineProject.sln"
  $buildTarget = if (Test-Path $solutionPath) { $solutionPath } else { $repo }
  $buildOut = & dotnet build $buildTarget -c Release 2>&1
  $buildText = ($buildOut | Out-String)
  $buildLines = $buildText -split "`r?`n"
  $buildWarnings = @($buildLines | Where-Object { $_ -match '(?i)\bwarning\s+[A-Z]{2}\d{4}\b' }).Count
  $buildErrors = @($buildLines | Where-Object { $_ -match '(?i)\berror\s+[A-Z]{2}\d{4}\b' }).Count
  $buildSucceeded = ($buildText -match '(?i)Build succeeded\.') -and -not ($buildText -match '(?i)Build FAILED\.')
  $warningCodes = @($buildLines | Select-String -Pattern '\bwarning\s+([A-Z]{2}\d{4})' -AllMatches | ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
  Write-Output "- Build warnings (approx.): $buildWarnings"
  Write-Output "- Build errors (approx.): $buildErrors"
  Write-Output "- Build succeeded: $buildSucceeded"
  if ($warningCodes.Count -gt 0) {
    Write-Output "- Warning codes: $([string]::Join(', ', $warningCodes))"
  }
}

if ($RunTests) {
  Write-Output "- Tests: running 'dotnet test -c Release' (this may take a moment)"
  $testOut = & dotnet test (Join-Path $repo "CarLine.Tests/CarLine.Tests.csproj") -c Release --collect:"XPlat Code Coverage" 2>&1
  $testText = ($testOut | Out-String)
  $summaryLine = ($testText -split "`r?`n" | Select-String -Pattern "Test summary:" | Select-Object -Last 1)
  if ($summaryLine) {
    Write-Output "- $($summaryLine.Line.Trim())"
  }
}

# Code coverage (if present)
$testResults = Join-Path $repo "CarLine.Tests\TestResults"
$coverage = Get-ChildItem -Path $testResults -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($coverage) {
  try {
    [xml]$xml = Get-Content -Raw $coverage.FullName
    $lineRate = [double]$xml.coverage."line-rate"
    $branchRate = [double]$xml.coverage."branch-rate"
    $linePct = [math]::Round($lineRate * 100, 2)
    $branchPct = [math]::Round($branchRate * 100, 2)
    $linesCovered = $xml.coverage."lines-covered"
    $linesValid = $xml.coverage."lines-valid"
    $branchesCovered = $xml.coverage."branches-covered"
    $branchesValid = $xml.coverage."branches-valid"
    $coverageRel = Get-RepoRelativePath $repo $coverage.FullName
    Write-Output "- Coverage (Cobertura): line-rate=$linePct% branch-rate=$branchPct%"
    if ($linesCovered -and $linesValid) {
      Write-Output "- Coverage details: lines $linesCovered/$linesValid, branches $branchesCovered/$branchesValid"
    }
    Write-Output "- Coverage file: $coverageRel"
  } catch {
    Write-Output "- Coverage: found file but failed to parse ($($coverage.FullName))"
    Write-Output "- Coverage parse error: $($_.Exception.Message)"
  }
} else {
  Write-Output "- Coverage: not found (run: dotnet test CarLine.Tests/CarLine.Tests.csproj -c Release --collect:\"XPlat Code Coverage\")"
}

Write-Section "Limitations"
Write-Output "- LOC is a size indicator, not a direct quality measure."
Write-Output "- Endpoint count is approximate (attribute-based count in controller files)."
Write-Output "- Coverage % depends on which assemblies are included by the test suite; low coverage indicates improvement areas."

Write-Output "- Build warnings: run 'dotnet build -c Release' and review warnings output (optional metric)"
