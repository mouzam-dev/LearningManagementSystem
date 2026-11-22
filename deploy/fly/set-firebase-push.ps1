#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Turn on phone push notifications: give the live site its Firebase service-account key.

.DESCRIPTION
    Reads the service-account JSON file you downloaded from Firebase (Project settings >
    Service accounts > Generate new private key), squeezes it onto one line and sets it as the
    Firebase__ServiceAccountJson secret on the Fly app. The key is sent to Fly over stdin, never
    printed. Delete the downloaded file afterwards - Fly keeps the only copy the site needs.

.EXAMPLE
    ./deploy/fly/set-firebase-push.ps1 -KeyFile "$HOME\Downloads\daura-firebase-adminsdk.json"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$KeyFile,
    [string]$App = 'duare-shariye'
)

$ErrorActionPreference = 'Stop'
if (-not (Get-Command fly -ErrorAction SilentlyContinue)) { throw "flyctl ('fly') is not installed or not on PATH." }
if (-not (Test-Path $KeyFile)) { throw "No file at '$KeyFile'." }

$key = Get-Content -Raw -Encoding UTF8 $KeyFile | ConvertFrom-Json
if ($key.type -ne 'service_account' -or -not $key.private_key -or -not $key.project_id) {
    throw "That isn't a Firebase service-account key. Download it from Firebase > Project settings > Service accounts."
}
$json = $key | ConvertTo-Json -Compress -Depth 5

$OutputEncoding = New-Object System.Text.UTF8Encoding $false   # no BOM in what fly reads
try {
    "Firebase__ServiceAccountJson=$json" | fly secrets import -a $App
    if ($LASTEXITCODE -ne 0) { throw "fly secrets import failed (exit $LASTEXITCODE)." }
    Write-Host "Done: push notifications are on for project '$($key.project_id)'. Fly is restarting the app."
}
finally {
    $json = $null; $key = $null
}
