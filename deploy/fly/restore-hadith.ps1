#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Copy the hadith tables from your local Postgres to the Fly database.

.DESCRIPTION
    Use this instead of "Refresh from source" on Fly: the in-app refresh takes ~17 min and
    Fly's auto-stop can kill it halfway. Refresh locally first, then run this script:

      1. Backs up "Hadiths" + "HadithCollections" from the local Docker Postgres (~18 MB).
      2. Checks the Fly database is on the same EF migration (aborts otherwise).
      3. Uploads the backup to the Fly Postgres machine and replaces those two tables in ONE
         transaction - if anything fails, Fly keeps its current hadith. Nothing else is touched.
      4. Restarts the app so its hadith cache reloads, then prints the live counts.

    Needs: docker (local DB running), flyctl logged in. No passwords are typed or printed:
    the Fly side uses the Postgres machine's own OPERATOR_PASSWORD.

.EXAMPLE
    ./deploy/fly/restore-hadith.ps1
.EXAMPLE
    ./deploy/fly/restore-hadith.ps1 -Yes      # skip the confirmation prompt
#>
[CmdletBinding()]
param(
    [string]$LocalContainer = 'nbs-postgres',
    [string]$LocalDb = 'lmsdb',
    [string]$LocalUser = 'postgres',
    [string]$PgApp = 'duare-shariye-pg',
    [string]$App = 'duare-shariye',
    [string]$RemoteDb = 'lmsdb',
    [switch]$Yes
)

$ErrorActionPreference = 'Stop'
function Assert-Ok([string]$step) { if ($LASTEXITCODE -ne 0) { throw "$step failed (exit $LASTEXITCODE)." } }

# Commands for a container go in a small sh file (LF endings, no BOM) that is copied in and run:
# Windows PowerShell mangles quotes in native arguments and adds a BOM to piped text.
function Write-ShFile([string]$path, [string]$body) {
    [IO.File]::WriteAllText($path, ($body -replace "`r`n", "`n"), (New-Object System.Text.UTF8Encoding $false))
}

foreach ($tool in 'docker', 'fly') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "'$tool' is not installed or not on PATH." }
}

$work = Join-Path ([IO.Path]::GetTempPath()) "hadith-restore-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $work | Out-Null
try {
    # --- 1. local backup ----------------------------------------------------------------
    Write-ShFile "$work/local.sh" @"
set -e
psql -U $LocalUser -d $LocalDb -At -c 'select max("MigrationId") from "__EFMigrationsHistory"'
psql -U $LocalUser -d $LocalDb -At -c 'select count(*) from "Hadiths"'
psql -U $LocalUser -d $LocalDb -At -c 'select count(*) from "HadithCollections"'
pg_dump -U $LocalUser -d $LocalDb --data-only -Fc -Z9 -t '"Hadiths"' -t '"HadithCollections"' -f /tmp/hadith.dump
"@
    docker cp "$work/local.sh" "${LocalContainer}:/tmp/hadith-local.sh"; Assert-Ok 'docker cp (script)'
    $info = @(docker exec $LocalContainer sh /tmp/hadith-local.sh); Assert-Ok 'Local backup'
    $migration, $hadiths, $collections = $info
    docker cp "${LocalContainer}:/tmp/hadith.dump" "$work/hadith.dump"; Assert-Ok 'docker cp (backup)'
    docker exec $LocalContainer rm -f /tmp/hadith.dump /tmp/hadith-local.sh | Out-Null
    Write-Host ("Local: {0} hadith in {1} collections (migration {2}); backup {3:N1} MB" -f `
        $hadiths, $collections, $migration, ((Get-Item "$work/hadith.dump").Length / 1MB))
    if ([int]$hadiths -eq 0) { throw 'The local Hadiths table is empty - run "Refresh from source" locally first.' }

    if (-not $Yes) {
        $answer = Read-Host "Replace the hadith on Fly app '$App' (database '$RemoteDb') with these? [y/N]"
        if ($answer -notmatch '^(y|yes)$') { Write-Host 'Cancelled.'; return }
    }

    # --- 2+3. remote restore script (LF line endings, or sh chokes on \r) --------------
    $remote = @'
#!/bin/sh
set -e
U="postgres://postgres:${OPERATOR_PASSWORD}@localhost:5432/$1"
cleanup() { rm -f /tmp/hadith.dump /tmp/hadith.sql /tmp/restore-hadith.sh; }
trap cleanup EXIT
have=$(psql "$U" -At -c 'select max("MigrationId") from "__EFMigrationsHistory"')
if [ "$have" != "$2" ]; then
  echo "ABORT: Fly database is on migration '$have' but the backup is from '$2'. Deploy the app first."
  exit 3
fi
pg_restore --data-only --no-owner -f /tmp/hadith.sql /tmp/hadith.dump
psql "$U" -v ON_ERROR_STOP=1 --single-transaction -q \
  -c 'TRUNCATE "Hadiths", "HadithCollections"' -f /tmp/hadith.sql > /dev/null
echo "Fly: $(psql "$U" -At -c 'select count(*) from "Hadiths"') hadith in $(psql "$U" -At -c 'select count(*) from "HadithCollections"') collections"
'@
    Write-ShFile "$work/restore-hadith.sh" $remote

    fly ssh sftp put -a $PgApp "$work/hadith.dump" /tmp/hadith.dump; Assert-Ok 'Upload backup'
    fly ssh sftp put -a $PgApp "$work/restore-hadith.sh" /tmp/restore-hadith.sh; Assert-Ok 'Upload script'
    fly ssh console -a $PgApp -C "sh /tmp/restore-hadith.sh $RemoteDb $migration"; Assert-Ok 'Restore on Fly'

    # --- 4. reload the app's cache and show what visitors now see ------------------------
    fly apps restart $App; Assert-Ok 'App restart'
    Start-Sleep -Seconds 10
    $live = Invoke-RestMethod "https://$App.fly.dev/api/sunnah/collections" -TimeoutSec 120
    Write-Host ("Live site: {0} hadith in {1} collections" -f ($live.data | Measure-Object totalHadith -Sum).Sum, $live.data.Count)
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
