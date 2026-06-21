# Deploying Duare Shariye to Fly.io

Two Fly apps in the **same region + org** so they share Fly's private network:

| App | What | Public? |
|---|---|---|
| `duare-shariye`    | Angular SPA + .NET API in one container (`fly.Dockerfile`) | yes → `https://<app>.fly.dev` |
| `duare-shariye-db` | SQL Server 2022 + a volume (`deploy/fly/db/fly.toml`)       | no — private `<app>.internal:1433` |

## Prerequisites (one-time)
- Install flyctl: `iwr https://fly.io/install.ps1 | iex` (Windows) — then `fly auth login`
- A Fly.io account with a card on file (the SQL Server machine needs a 2 GB VM)

> App names are **globally unique**. If `duare-shariye*` is taken, pick your own and
> pass it with `-a <name>` (and update the connection string accordingly).

## 1 — SQL Server (private)
```powershell
$DB="duare-shariye-db"; $REGION="bom"          # pick your region; use it for BOTH apps
fly apps create $DB
fly volume create mssql_data --size 3 --region $REGION -a $DB
fly secrets set MSSQL_SA_PASSWORD='<StrongPassword!>' -a $DB
fly deploy -c deploy/fly/db/fly.toml -a $DB
```

## 2 — Web app (SPA + API)
```powershell
$APP="duare-shariye"
fly apps create $APP
# Generate a JWT signing key (any 32+ char secret):
$JWT = -join ((1..64) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) })
fly secrets set `
  "ConnectionStrings__DefaultConnection=Server=duare-shariye-db.internal,1433;Database=LmsDb;User Id=sa;Password=<StrongPassword!>;TrustServerCertificate=True;MultipleActiveResultSets=true" `
  "Jwt__Key=$JWT" `
  "Sunnah__ApiKey=<your sunnah.com key>" `
  "App__PublicWebUrl=https://$APP.fly.dev" `
  -a $APP
fly deploy -c fly.toml -a $APP
```
On first boot the app **creates `LmsDb`, applies every migration**, and starts. Watch it: `fly logs -a $APP`.

## 3 — Load the hadith
Open `https://<app>.fly.dev`, sign in as a **SuperAdmin**, then **Admin → Hadith → Refresh from source**. The container harvests ~46k hadith from sunnah.com + jsdelivr (this works on Fly; it was blocked on the old free Windows host).

## Notes
- Keep both apps in the **same `primary_region`**.
- The web app scales to zero when idle (`min_machines_running = 0`); the DB stays on.
- Main cost is the 2 GB SQL machine. To trim later: `MSSQL_PID=Express` is already the free, production-licensed edition.
- Redeploys: just `fly deploy` again — only *new* migrations run on boot.
