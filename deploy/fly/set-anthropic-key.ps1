#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Turn on AI answers and notes in the Qur'an & Hadith library: give the live site an Anthropic API key.

.DESCRIPTION
    Create a key at https://console.anthropic.com (Settings > API keys), then run this script and paste it
    when asked. The key is typed hidden, sent to Fly over stdin as the Anthropic__ApiKey secret, and never
    printed. Fly restarts the app; the library's "Ask" and "Make notes" tabs then work.
    Optional: -DailyLimit changes how many AI requests each user gets per day (default 20).

.EXAMPLE
    ./deploy/fly/set-anthropic-key.ps1
    ./deploy/fly/set-anthropic-key.ps1 -DailyLimit 30
#>
[CmdletBinding()]
param(
    [string]$App = 'duare-shariye',
    [int]$DailyLimit = 0
)

$ErrorActionPreference = 'Stop'
if (-not (Get-Command fly -ErrorAction SilentlyContinue)) { throw "flyctl ('fly') is not installed or not on PATH." }

$secure = Read-Host 'Paste your Anthropic API key (input is hidden)' -AsSecureString
$key = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
try {
    $key = $key.Trim()
    if (-not $key.StartsWith('sk-ant-')) { throw "That doesn't look like an Anthropic API key (it should start with sk-ant-)." }

    $lines = @("Anthropic__ApiKey=$key")
    if ($DailyLimit -gt 0) { $lines += "Library__AiDailyLimit=$DailyLimit" }
    $OutputEncoding = New-Object System.Text.UTF8Encoding $false
    $lines -join "`n" | fly secrets import -a $App
    if ($LASTEXITCODE -ne 0) { throw "fly secrets import failed (exit $LASTEXITCODE)." }
    Write-Host 'Done: AI answers are on. Fly is restarting the app.'
}
finally {
    $key = $null; $secure = $null
}
