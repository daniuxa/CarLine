param(
  [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
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

$tracked = Get-TrackedFiles $repo

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

Write-Output "- Top 5 largest tracked files (selected extensions):"
$top5 = $lineCounts | Sort-Object Lines -Descending | Select-Object -First 5
foreach ($t in $top5) {
  Write-Output "  - $($t.Path): $($t.Lines)"
}

Write-Section "Quality"

# Tests
$testDllCount = ($tracked | Where-Object { $_ -like "*CarLine.Tests*" -and $_.EndsWith(".cs") } | Measure-Object).Count
Write-Output "- Test files (.cs under CarLine.Tests): $testDllCount"

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
    $coverageRel = $coverage.FullName
    $repoNorm = $repo.TrimEnd('\','/')
    if ($coverageRel.ToLowerInvariant().StartsWith($repoNorm.ToLowerInvariant())) {
      $coverageRel = $coverageRel.Substring($repoNorm.Length).TrimStart('\','/')
    }
    $coverageRel = $coverageRel.Replace('\','/')
    Write-Output "- Coverage (Cobertura): line-rate=$linePct% branch-rate=$branchPct%"
    Write-Output "- Coverage file: $coverageRel"
  } catch {
    Write-Output "- Coverage: found file but failed to parse ($($coverage.FullName))"
    Write-Output "- Coverage parse error: $($_.Exception.Message)"
  }
} else {
  Write-Output "- Coverage: not found (run: dotnet test CarLine.Tests/CarLine.Tests.csproj -c Release --collect:\"XPlat Code Coverage\")"
}

Write-Output "- Build warnings: run 'dotnet build -c Release' and review warnings output (optional metric)"
