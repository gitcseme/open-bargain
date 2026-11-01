# Deploying to Azure

How the Azure resources for Open Bargain were created (task M0-5). Everything is done in the Azure portal (https://portal.azure.com).

## Overview

```
GitHub Actions ──deploys──▶ Web App (runs on an App Service plan)
                                 │  reads ConnectionStrings__Default from its settings
                                 ▼
                      PostgreSQL Flexible Server
        (all inside one resource group)
```

The app and database are in **Southeast Asia**. Central India was tried first, but PostgreSQL B1ms isn't available there for this subscription (`SkuNotAvailable`). The resource group's own region (Central India) only says where its metadata is stored; it can hold resources from any region.

| Resource | Name |
|---|---|
| Resource group | `rg-open-bargain` |
| App Service plan | `asp-openbargain` (Linux, Free F1) |
| Web App | `open-bargain` → https://open-bargain-efb0gkdfczesg4dw.southeastasia-01.azurewebsites.net |
| PostgreSQL server | `openbargain-2026` → `openbargain-2026.postgres.database.azure.com` (Burstable B1ms, 32 GiB, PostgreSQL 17), admin `obadmin2026`, database `openbargain` |

## 1. Resource group

A resource group is a folder for resources. Deleting it deletes everything inside.

1. **Resource groups** → **+ Create**.
2. Name `rg-open-bargain`, region Central India.
3. **Review + create** → **Create**.

## 2. App Service plan and Web App

The plan is the machine you pay for; the Web App is the app running on it.

1. **App Services** → **+ Create** → **Web App**.
2. **Basics:** resource group `rg-open-bargain`; publish **Code**; runtime **.NET 10 (LTS)**; OS **Linux**; region **Southeast Asia** (same as the database); new Linux plan `asp-openbargain`; pricing plan **Free F1**.
   - F1 limits: 60 CPU minutes a day, the app sleeps when idle (slow first request). Move to Basic B1 before real use.
3. **Database:** none (PostgreSQL is created separately in step 3).
4. **Deployment:** continuous deployment **Disable** (we use our own workflow); basic authentication **Enable** (needed for the publish profile).
5. **Monitoring:** Application Insights **No** for now.
6. **Review + create** → **Create**. The default domain shows Azure's welcome page until the first deploy.

## 3. PostgreSQL Flexible Server

1. **Azure Database for PostgreSQL flexible servers** → **+ Create**.
2. **Basics:** resource group `rg-open-bargain`; server name `openbargain-2026`; region **Southeast Asia**; PostgreSQL **17**; workload **Development**.
   - **Configure server:** Burstable **Standard_B1ms**, **32 GiB**, storage autogrow **off**, high availability **off**, 7-day backups, geo-redundant backup **off**.
   - **Authentication:** PostgreSQL authentication only; admin `obadmin2026`; keep the password in a password manager, never in the repo.
3. **Networking:** public access; tick **Allow public access from any Azure service within Azure to this server** (without it the Web App can't connect). Optionally add your own IP to connect from your PC.
4. **Review + create:** about USD 22/month (compute ~18, storage ~4). Stop the server when not in use to pause compute billing; Azure starts it again automatically after 7 days.
5. When it is created: **Settings → Databases → + Add** → `openbargain`.

`SkuNotAvailable` means that size isn't offered in that region for your subscription. Try another region; `az postgres flexible-server list-skus --location <region>` shows what is available.

## 4. Connection string

The app reads `ConnectionStrings:Default`. On Linux, environment variables can't contain `:`, so .NET also accepts `__`. An app setting named `ConnectionStrings__Default` overrides the local value in `appsettings.json`. (The portal's *Connection strings* tab isn't used: .NET doesn't map its PostgreSQL entries automatically.)

1. Web App → **Settings → Environment variables → App settings** → **+ Add**.
2. Name `ConnectionStrings__Default`, value:
   ```
   Host=openbargain-2026.postgres.database.azure.com;Port=5432;Database=openbargain;Username=obadmin2026;Password=<PASSWORD>;SSL Mode=Require
   ```
   If the password contains `;` or quotes, wrap it: `Password="..."`.
3. **Apply** → **Apply** → **Confirm**. The app restarts.

`/api/health` showing `"database":"ok"` after the first deploy confirms this step. If it fails, check this value and the PostgreSQL networking setting from step 3.

## 5. Publish profile → GitHub secret

The publish profile is an XML file with a username and password that can deploy to this Web App. Keep it secret.

1. Web App → **Settings → Configuration → General settings**: **SCM Basic Auth Publishing Credentials** must be **On**.
2. Web App → **Overview** → **Download publish profile**.
3. GitHub repo → **Settings → Secrets and variables → Actions** → **New repository secret**: name `AZURE_WEBAPP_PUBLISH_PROFILE`, value = the whole file. Or:
   `gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --repo gitcseme/open-bargain < <file>.PublishSettings`
4. Delete the downloaded file. If it leaks: Web App → **Overview** → **Reset publish profile**, then update the secret.

## 6. Deploy workflow

`.github/workflows/deploy.yml` runs when a pull request from `develop` into `main` is **merged**, or manually from `main` (**Actions → Deploy → Run workflow**, branch `main`), for example to redeploy after changing an Azure setting. Direct pushes to `main`, PRs from other branches, PRs closed without merging and manual runs on other branches don't deploy. It:

1. builds the frontend into `src/OpenBargain.Api/wwwroot`
2. runs `dotnet publish`, which includes `wwwroot`
3. uploads the result to the Web App with `azure/webapps-deploy` and the publish profile secret

Tests aren't repeated here: changes reach `main` only through pull requests, and the CI checks must pass first.

Check after a deploy: `<web app URL>/api/health` returns `{"status":"ok","database":"ok"}`.
