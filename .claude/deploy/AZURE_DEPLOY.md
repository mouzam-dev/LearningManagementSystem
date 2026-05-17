# Deploying the LMS to Azure (Free Tier) — Step by Step

This guide walks you through deploying the LMS to Azure using **only free-tier services**, so you can share a live demo URL with stakeholders. The whole walk-through takes about **45 minutes** end-to-end (most of it waiting for Azure to provision).

**Final architecture:**

```
Browser → https://lms-demo-<suffix>.azurewebsites.net
                        │
              ┌─────────┴──────────┐
              │  Azure App Service │   ← serves Angular SPA + ASP.NET Core API
              │  (Free F1 plan)    │     same origin, no CORS
              └─────────┬──────────┘
                        │
              ┌─────────┴──────────┐
              │  Azure SQL DB      │   ← schema auto-migrated, sample data
              │  (Free tier, 32GB) │     auto-seeded on first boot
              └────────────────────┘
```

Demo logins (password `Password1!` for all):
- `demo.admin@lms.dev` — Super Administrator
- `demo.orgadmin@lms.dev` — Organization Admin
- `demo.teacher@lms.dev` — Teacher
- `demo.student@lms.dev` — Student

---

## What you'll need

- An **Azure account** (the [free trial](https://azure.microsoft.com/free) gives $200 credit + 12 months of selected services free)
- **Azure CLI** installed on your laptop ([install instructions](https://learn.microsoft.com/cli/azure/install-azure-cli))
- **.NET 8 SDK** ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Node.js 18+** ([download](https://nodejs.org))
- **PowerShell 7+** (already on Windows; on macOS/Linux: `brew install powershell`)
- A clone of this repository

To confirm prerequisites, run:

```powershell
az --version          # 2.50 or newer
dotnet --version      # 8.0.x (don't worry if the latest is 9 or 10 — 8 is fine)
node --version        # v18 or v20
pwsh --version        # 7.x
```

---

## Phase 1 — One-time Azure setup (≈ 10 min)

### 1.1 Sign in to Azure

```powershell
az login
```

A browser window opens — sign in with your Azure account. The command returns a JSON list of your subscriptions. Note the `id` of the subscription you want to use (most accounts only have one).

If you have multiple subscriptions:

```powershell
az account set --subscription "<subscription-id-or-name>"
```

### 1.2 Pick a unique suffix for resource names

App Service URLs are global, so the name has to be unique across all of Azure. We'll use a 4-character suffix everywhere — replace `xyz1` with anything that hasn't been claimed:

```powershell
$suffix    = "xyz1"           # ← change me
$region    = "eastus"         # or any region close to your audience
$rgName    = "lms-demo-rg"
$sqlServer = "lms-sql-$suffix"
$sqlDb     = "lmsdb"
$plan      = "lms-plan-$suffix"
$webApp    = "lms-demo-$suffix"
$sqlAdmin  = "lmsadmin"
$sqlPwd    = "$(New-Guid)!"   # auto-generates a strong password
```

Keep this PowerShell window open — the variables are reused throughout.

### 1.3 Create the resource group

```powershell
az group create --name $rgName --location $region
```

Everything we create from here on lives inside this resource group, so cleanup is one command (Phase 5).

---

## Phase 2 — Database (≈ 5 min)

### 2.1 Create the SQL Server logical server

```powershell
az sql server create `
    --name $sqlServer `
    --resource-group $rgName `
    --location $region `
    --admin-user $sqlAdmin `
    --admin-password $sqlPwd

Write-Host "SQL admin password: $sqlPwd" -ForegroundColor Yellow
```

Save the printed password — you'll need it if you ever want to connect from SSMS / Azure Data Studio.

### 2.2 Create the database on the **Free** tier

Azure SQL Database has a free offer (introduced 2024): 32 GB storage, ~100,000 vCore-seconds per month, one free DB per subscription. The server auto-pauses when idle (~5-10s cold start when it wakes), which is fine for a demo.

```powershell
az sql db create `
    --resource-group $rgName `
    --server $sqlServer `
    --name $sqlDb `
    --use-free-limit `
    --free-limit-exhaustion-behavior AutoPause
```

> If `--use-free-limit` errors with "free offer not available," your subscription may already have a free DB elsewhere — check the [free offer eligibility](https://learn.microsoft.com/azure/azure-sql/database/free-offer). Fallback: omit those two flags and use `--service-objective Basic` (~$5/mo).

### 2.3 Open the firewall

By default Azure SQL blocks everything. Allow Azure services (so the App Service can connect) and your own IP (so you can run migrations / inspect data):

```powershell
# Let other Azure services in (App Service)
az sql server firewall-rule create `
    --resource-group $rgName `
    --server $sqlServer `
    --name AllowAzureServices `
    --start-ip-address 0.0.0.0 `
    --end-ip-address 0.0.0.0

# Let your own IP in
$myIp = (Invoke-RestMethod -Uri "https://api.ipify.org")
az sql server firewall-rule create `
    --resource-group $rgName `
    --server $sqlServer `
    --name AllowMyIp `
    --start-ip-address $myIp `
    --end-ip-address $myIp
```

### 2.4 Capture the connection string

```powershell
$sqlConn = "Server=tcp:$sqlServer.database.windows.net,1433;Database=$sqlDb;User ID=$sqlAdmin;Password=$sqlPwd;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
Write-Host $sqlConn
```

This is what the API will use as `ConnectionStrings:DefaultConnection`. We'll set it as an App Service setting in Phase 3.

---

## Phase 3 — App Service (≈ 5 min)

### 3.1 Create the Free F1 App Service plan

The Free F1 plan is shared compute with these limits:
- 1 GB RAM, 1 GB disk
- 60 CPU-minutes per day (resets at midnight UTC)
- App idles after 20 min of no traffic (~5-10 sec cold start)
- No custom domain SSL (the `*.azurewebsites.net` cert is fine for a demo)

```powershell
az appservice plan create `
    --name $plan `
    --resource-group $rgName `
    --sku F1 `
    --is-linux
```

> Skip `--is-linux` if you'd rather run on Windows App Service. Both work for this app; Linux F1 starts faster.

### 3.2 Create the Web App on the .NET 8 runtime

```powershell
az webapp create `
    --resource-group $rgName `
    --plan $plan `
    --name $webApp `
    --runtime "DOTNETCORE:8.0"
```

The URL is now reserved: `https://$webApp.azurewebsites.net` (it returns a "Welcome to App Service" page until we deploy).

### 3.3 Set application settings

The app reads its connection string, JWT signing key, and CORS allowlist from configuration. Set them as App Service settings:

```powershell
# Long random JWT key. Save this somewhere — if you change it, every issued
# token instantly becomes invalid.
$jwtKey = ([Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N"))

az webapp config appsettings set `
    --resource-group $rgName `
    --name $webApp `
    --settings `
        "ASPNETCORE_ENVIRONMENT=Production" `
        "Jwt__Key=$jwtKey" `
        "Jwt__Issuer=LmsApp" `
        "Jwt__Audience=LmsUsers" `
        "App__PublicWebUrl=https://$webApp.azurewebsites.net" `
        "LMS_SEED_SAMPLE=true"

# Connection string goes in a separate slot so App Service knows it's a
# connection string (shown masked in the portal).
az webapp config connection-string set `
    --resource-group $rgName `
    --name $webApp `
    --connection-string-type SQLAzure `
    --settings DefaultConnection="$sqlConn"
```

Verify they landed:

```powershell
az webapp config appsettings list --resource-group $rgName --name $webApp --output table
```

---

## Phase 4 — Build & deploy the app (≈ 15 min, mostly waiting)

The repo ships a one-shot build script that produces a single zip ready for App Service.

### 4.1 Run the build script

From the repo root:

```powershell
./deploy/build-azure-bundle.ps1
```

This:
1. Runs `npm install` if needed
2. Builds the Angular app for production (`/api` as base URL, `/` as base href) into `src/LMS.WebAPI/wwwroot/`
3. Runs `dotnet publish -c Release` for the API
4. Zips the publish output to `./deploy/lms-azure.zip`

Expected output ends with:

```
✓ Built ./deploy/lms-azure.zip  (~15 MB)
```

### 4.2 Deploy the zip

```powershell
az webapp deploy `
    --resource-group $rgName `
    --name $webApp `
    --src-path ./deploy/lms-azure.zip `
    --type zip
```

This takes 2–4 minutes. When the command returns, the app is starting.

### 4.3 First boot — what happens

When the app boots for the first time, the startup pipeline does:

1. EF Core applies every migration → creates all tables in the Azure SQL DB
2. `TenancySeeder` seeds the Permissions, Role-Permissions, and Default Organization
3. `SampleDataSeeder` notices the catalog is empty and inserts 3 orgs, ~80 users, 15 courses, lessons, assessments, ~180 enrollments, ~30 certificates (≈ 30–60 seconds because BCrypt password hashing is intentionally slow)

You can watch this happen in the App Service log stream:

```powershell
az webapp log tail --resource-group $rgName --name $webApp
```

Look for `SampleDataSeeder: done — 3 orgs, 82 users, 15 courses.`

### 4.4 Open the URL

```powershell
Start-Process "https://$webApp.azurewebsites.net"
```

If you see the **Sign in** page, you're done. Sign in with `demo.admin@lms.dev` / `Password1!`.

> **First-visit cold start:** F1 takes 10-20 seconds the first time, plus 5-10 seconds extra if the SQL DB has auto-paused. Subsequent loads are fast.

---

## Phase 5 — Sharing the demo

### Demo URLs to send

| Role | URL | Login |
|---|---|---|
| Sign in page | `https://<webApp>.azurewebsites.net/login` | — |
| Super Admin dashboard | `…/admin/dashboard` | `demo.admin@lms.dev` |
| Org Admin dashboard | `…/orgadmin/dashboard` | `demo.orgadmin@lms.dev` |
| Teacher dashboard | `…/teacher/dashboard` | `demo.teacher@lms.dev` |
| Student dashboard | `…/student/dashboard` | `demo.student@lms.dev` |
| API swagger (Production hides it by default — flip `ASPNETCORE_ENVIRONMENT=Development` to expose, then back) | `…/swagger` | n/a |

All seeded passwords are `Password1!`.

### Send stakeholders the user manual

The `.claude/user-manual/LMS_User_Manual.pdf` (22 pages) is the matching reference doc — share it alongside the URL.

---

## Phase 6 — Keeping it healthy during the demo

### Stop the App Service after the demo to save CPU minutes

```powershell
az webapp stop --resource-group $rgName --name $webApp
```

To bring it back:

```powershell
az webapp start --resource-group $rgName --name $webApp
```

### View live logs

```powershell
az webapp log tail --resource-group $rgName --name $webApp
```

### Check resource costs

The free tiers are genuinely free, but it's worth checking nothing accidentally got upgraded:

```powershell
az consumption usage list --start-date (Get-Date).AddDays(-7).ToString("yyyy-MM-dd") --end-date (Get-Date).ToString("yyyy-MM-dd")
```

---

## Phase 7 — Updates & redeploys

Whenever you change the code:

```powershell
# Rebuild
./deploy/build-azure-bundle.ps1

# Redeploy (same command as the first time)
az webapp deploy `
    --resource-group $rgName `
    --name $webApp `
    --src-path ./deploy/lms-azure.zip `
    --type zip
```

EF Core only runs *new* migrations on each boot — old ones are skipped, so this is safe.

---

## Phase 8 — Tearing it all down

When you're done demoing and want to remove every Azure resource (and any charges):

```powershell
az group delete --name $rgName --yes --no-wait
```

This deletes the SQL server, the database, the App Service plan, and the Web App in one shot. Returns immediately; cleanup takes 3–5 minutes in the background.

---

## Troubleshooting

### "Login failed for user 'lmsadmin'"

The App Service can't reach SQL. Check:

1. The firewall rule `AllowAzureServices` (start=0.0.0.0, end=0.0.0.0) exists:
   ```powershell
   az sql server firewall-rule list --resource-group $rgName --server $sqlServer --output table
   ```
2. The connection string in App Service config matches:
   ```powershell
   az webapp config connection-string list --resource-group $rgName --name $webApp
   ```

### "Container didn't start in expected time" / Web App returns 503

App Service health probe times out on first boot because BCrypt + seeding takes ~60s on the free tier. Wait 90 seconds and refresh — the seeder always completes.

If it persists, tail the logs:

```powershell
az webapp log tail --resource-group $rgName --name $webApp
```

Look for unhandled exceptions. If you see `Jwt:Key not configured`, you missed the `Jwt__Key` setting in Phase 3.3.

### "Daily CPU quota exceeded"

The F1 plan caps at 60 CPU-minutes/day. The app gets throttled with HTTP 429s until midnight UTC. Either wait it out, stop the app (`az webapp stop`) until you need it again, or upgrade to the Basic B1 plan (~$13/mo):

```powershell
az appservice plan update --name $plan --resource-group $rgName --sku B1
```

### "Image upload failed: 413 Payload Too Large"

App Service Linux has a 30 MB request limit by default. The API caps images at 5 MB so this shouldn't fire — if it does, the bug is on the client (giant base64 payload). Open browser DevTools → Network and check the request size.

### The SPA loads but every API call returns 404

The Angular build didn't land in `wwwroot` correctly, OR `apiUrl` is pointing somewhere else. Check:

- The bundle includes `wwwroot/index.html` (unzip `./deploy/lms-azure.zip` and look)
- The Angular build wrote `main-*.js` referencing `/api`, not `http://localhost:5117/api`

If the `environment.prod.ts` was edited, fix it back to `apiUrl: '/api'` and rebuild.

### I need to wipe the database and reseed

```powershell
# 1) Stop the App Service so it doesn't race the seeder
az webapp stop --resource-group $rgName --name $webApp

# 2) Drop + recreate the database
az sql db delete --resource-group $rgName --server $sqlServer --name $sqlDb --yes
az sql db create  --resource-group $rgName --server $sqlServer --name $sqlDb --use-free-limit --free-limit-exhaustion-behavior AutoPause

# 3) Start the app again — first boot will reseed
az webapp start --resource-group $rgName --name $webApp
```

### I want a custom domain

The Free F1 tier doesn't support custom domain SSL. Upgrade to Basic B1+ first:

```powershell
az appservice plan update --name $plan --resource-group $rgName --sku B1
az webapp config hostname add --resource-group $rgName --webapp-name $webApp --hostname demo.example.com
az webapp config ssl bind --resource-group $rgName --name $webApp ...
```

See [the Microsoft docs for the full SSL flow](https://learn.microsoft.com/azure/app-service/configure-ssl-certificate).

---

## Quick reference — every command

For copy-paste convenience, the whole sequence with `xyz1` as the suffix:

```powershell
# Variables
$suffix    = "xyz1"
$region    = "eastus"
$rgName    = "lms-demo-rg"
$sqlServer = "lms-sql-$suffix"
$sqlDb     = "lmsdb"
$plan      = "lms-plan-$suffix"
$webApp    = "lms-demo-$suffix"
$sqlAdmin  = "lmsadmin"
$sqlPwd    = "$(New-Guid)!"
$jwtKey    = ([Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N"))

# Resource group
az group create --name $rgName --location $region

# SQL Server + Free DB + firewall
az sql server create --name $sqlServer --resource-group $rgName --location $region --admin-user $sqlAdmin --admin-password $sqlPwd
az sql db create --resource-group $rgName --server $sqlServer --name $sqlDb --use-free-limit --free-limit-exhaustion-behavior AutoPause
az sql server firewall-rule create --resource-group $rgName --server $sqlServer --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
$myIp = (Invoke-RestMethod -Uri "https://api.ipify.org")
az sql server firewall-rule create --resource-group $rgName --server $sqlServer --name AllowMyIp --start-ip-address $myIp --end-ip-address $myIp
$sqlConn = "Server=tcp:$sqlServer.database.windows.net,1433;Database=$sqlDb;User ID=$sqlAdmin;Password=$sqlPwd;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# App Service plan + Web App
az appservice plan create --name $plan --resource-group $rgName --sku F1 --is-linux
az webapp create --resource-group $rgName --plan $plan --name $webApp --runtime "DOTNETCORE:8.0"

# Settings
az webapp config appsettings set --resource-group $rgName --name $webApp --settings "ASPNETCORE_ENVIRONMENT=Production" "Jwt__Key=$jwtKey" "Jwt__Issuer=LmsApp" "Jwt__Audience=LmsUsers" "App__PublicWebUrl=https://$webApp.azurewebsites.net" "LMS_SEED_SAMPLE=true"
az webapp config connection-string set --resource-group $rgName --name $webApp --connection-string-type SQLAzure --settings DefaultConnection="$sqlConn"

# Build + deploy
./deploy/build-azure-bundle.ps1
az webapp deploy --resource-group $rgName --name $webApp --src-path ./deploy/lms-azure.zip --type zip

# Open it
Start-Process "https://$webApp.azurewebsites.net"

# Show credentials you should save
Write-Host "URL:        https://$webApp.azurewebsites.net" -ForegroundColor Green
Write-Host "SQL admin:  $sqlAdmin / $sqlPwd"               -ForegroundColor Yellow
Write-Host "Jwt:Key:    $jwtKey"                            -ForegroundColor Yellow
```

Tear-down:

```powershell
az group delete --name $rgName --yes --no-wait
```

---

*Last updated: 2026-05-17 · Tested against Azure CLI 2.65, .NET 8.0.x, Node 20.x.*
