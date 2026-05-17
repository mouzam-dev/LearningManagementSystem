#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build the LMS into a single zip ready to deploy to Azure App Service.

.DESCRIPTION
    1. Builds the Angular app for production with relative apiUrl '/api'.
    2. Copies the Angular dist into src/LMS.WebAPI/wwwroot (alongside the
       existing /uploads folder).
    3. Runs `dotnet publish` for the API in Release mode.
    4. Zips the publish output to ./deploy/lms-azure.zip - ready for
       `az webapp deploy --src-path ./deploy/lms-azure.zip`.

.PARAMETER OutputZip
    Path of the zip file to produce. Defaults to ./deploy/lms-azure.zip.

.EXAMPLE
    ./deploy/build-azure-bundle.ps1
#>
[CmdletBinding()]
param(
    [string]$OutputZip = "./deploy/lms-azure.zip"
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path "$PSScriptRoot/.."
Set-Location $repoRoot

$wwwroot       = "$repoRoot/src/LMS.WebAPI/wwwroot"
$ngDistRoot    = "$repoRoot/lms-angular/dist/lms-angular"
$publishDir    = "$repoRoot/src/LMS.WebAPI/bin/Release/net8.0/publish"

Write-Host "[1/4] Building Angular (production)..." -ForegroundColor Cyan
Push-Location "$repoRoot/lms-angular"
try {
    if (-not (Test-Path node_modules)) {
        Write-Host "    node_modules missing - running npm install" -ForegroundColor Yellow
        npm install --no-audit --no-fund
    }
    # Clean previous dist so we know exactly what landed.
    if (Test-Path "dist") { Remove-Item -Recurse -Force "dist" }
    npx --no-install ng build --configuration production --base-href "/"
}
finally { Pop-Location }

Write-Host "[2/4] Copying Angular browser output into wwwroot..." -ForegroundColor Cyan
$ngBrowser = "$ngDistRoot/browser"
if (-not (Test-Path "$ngBrowser/index.html")) {
    throw "Angular did not emit $ngBrowser/index.html - check the build output above."
}
# Wipe stale Angular artifacts from wwwroot but keep uploads/ (runtime user content).
if (Test-Path $wwwroot) {
    Get-ChildItem -LiteralPath $wwwroot -Force |
        Where-Object { $_.Name -ne 'uploads' } |
        ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
} else {
    New-Item -ItemType Directory -Path $wwwroot | Out-Null
}
Copy-Item -Path "$ngBrowser/*" -Destination $wwwroot -Recurse -Force
if (-not (Test-Path "$wwwroot/index.html")) {
    throw "wwwroot/index.html not present after copy."
}

Write-Host "[3/4] Publishing API (dotnet publish -c Release)…" -ForegroundColor Cyan
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
dotnet publish "$repoRoot/src/LMS.WebAPI/LMS.WebAPI.csproj" `
    --configuration Release `
    --nologo `
    --verbosity minimal

Write-Host "[4/4] Zipping publish output -> $OutputZip" -ForegroundColor Cyan
if (Test-Path $OutputZip) { Remove-Item -Force $OutputZip }
$OutputZipDir = Split-Path -Parent (Resolve-Path -LiteralPath . | Join-Path -ChildPath $OutputZip)
if (-not (Test-Path $OutputZipDir)) { New-Item -ItemType Directory -Path $OutputZipDir | Out-Null }

# Compress-Archive needs absolute paths and the destination directory existing.
$absoluteZip = Join-Path (Resolve-Path .) $OutputZip
Compress-Archive -Path "$publishDir/*" -DestinationPath $absoluteZip -Force

$zipSizeMb = [Math]::Round((Get-Item $absoluteZip).Length / 1MB, 2)
Write-Host ""
Write-Host "Built $OutputZip  ($zipSizeMb MB)" -ForegroundColor Green
Write-Host ""
Write-Host "Next: deploy to App Service" -ForegroundColor White
Write-Host "    az webapp deploy --resource-group <rg> --name <app> --src-path $OutputZip --type zip" -ForegroundColor Gray
