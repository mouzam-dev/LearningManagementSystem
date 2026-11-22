#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Point the live site's email (password reset, email verification) at a Gmail account.

.DESCRIPTION
    Asks for the Gmail address and its 16-character app password (typed hidden, never shown or
    saved to disk), then sets the Smtp__* secrets on the Fly app. Fly restarts the app with them.

    Before running: turn on 2-Step Verification for the Gmail account, then create an app
    password at https://myaccount.google.com/apppasswords.

.EXAMPLE
    ./deploy/fly/set-gmail-smtp.ps1
#>
[CmdletBinding()]
param(
    [string]$App = 'duare-shariye',
    [string]$Address = 'quranhalqahadmin@gmail.com'   # the site's sending mailbox
)

$ErrorActionPreference = 'Stop'
if (-not (Get-Command fly -ErrorAction SilentlyContinue)) { throw "flyctl ('fly') is not installed or not on PATH." }

$typed = (Read-Host "Gmail address to send from [press Enter for $Address]").Trim()
$address = if ($typed) { $typed.ToLowerInvariant() } else { $Address }
if ($address -notmatch '^[^@\s]+@[^@\s]+\.[^@\s]+$') { throw "'$address' doesn't look like an email address." }

$secure = Read-Host 'Gmail app password (16 characters, typing is hidden)' -AsSecureString
$plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
$plain = $plain -replace '\s', ''   # Google shows it in groups of four; the spaces aren't part of it
if ($plain.Length -ne 16) { throw 'An app password is 16 characters. Create one at https://myaccount.google.com/apppasswords.' }

try {
    fly secrets set -a $App `
        'Smtp__Host=smtp.gmail.com' 'Smtp__Port=587' 'Smtp__EnableSsl=true' `
        "Smtp__Username=$address" "Smtp__Password=$plain" "Smtp__FromAddress=$address"
    if ($LASTEXITCODE -ne 0) { throw "fly secrets set failed (exit $LASTEXITCODE)." }
    Write-Host "Done: '$App' now sends email as $address. Fly is restarting the app."
}
finally {
    $plain = $null
}
