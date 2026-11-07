# Hadm (هضم)

Hadm is a private digestive symptom diary. It helps people record meals, symptoms, bowel movements, and daily context, then review their entries with a clinician. It does not diagnose conditions or recommend treatment.

## Run locally

Requirements: .NET 10 SDK, SQL Server LocalDB (or SQL Server Express), and Node.js/npm.

1. Start the API: `dotnet run --project src/Hadm.WebApi`.
2. In another terminal, start the Angular app: `cd client && npm start`.
3. Open `http://localhost:4200`. The development server proxies `/api` requests to `http://localhost:5292`.

The API applies EF Core migrations and seeds built-in symptom and food lists on startup. The initial schema is in `Hadm.Infrastructure/Persistence/Migrations`. Override `ConnectionStrings:Hadm` to use another SQL Server instance. Arabic PDF text uses the operating system fonts; set `HADM_FONT_DIR` to a directory containing Noto Naskh Arabic or Noto Sans Arabic if the host lacks Arabic fonts. The development JWT signing key is only for local use. Set `Jwt__Key` to a private value of at least 32 bytes in deployed environments, and configure `Jwt__Issuer`, `Jwt__Audience`, and `ClientOrigins` for the deployment.

## Included MVP

- Email and password registration/login with ASP.NET Core Identity and JWT authentication.
- Meal, symptom, bowel movement, and daily context logging.
- User-scoped timeline, severity trend, and food correlation observations with minimum-data thresholds and sample counts.
- English and Arabic interface with right-to-left layout.
- Red-flag notices and a medical disclaimer.
- On-demand PDF report and account/data deletion.
- Optional AI-assisted English/Arabic diary parsing with user-confirmed drafts, weekly insight summaries, report summaries, and note warning scans. AI stays off until the user explicitly opts in.

## Optional AI provider

AI features use a server-side, provider-swappable IAiService. The current adapter calls Anthropic's Messages API; the API key stays on the server and is never sent to Angular. Configure Ai__Anthropic__ApiKey as an environment secret. Ai__Anthropic__Model is optional and defaults to claude-haiku-5-5. Without a key, the UI reports that AI is unavailable while non-AI logging, deterministic insights, and reports continue to work.

Users must enable AI in Settings before diary text or anonymous aggregate counts are sent. Natural-language parses are validated against the user's catalog and returned as editable drafts; no entry is persisted until the user confirms. Weekly and report summaries contain only SQL-computed counts, and weekly summaries are cached briefly. AI requests are rate limited per account. Prompts are fixed on the server and instruct the model not to diagnose or recommend treatment.

For local development in PowerShell, set Ai__Anthropic__ApiKey as an environment variable before starting the API, for example:

    $env:Ai__Anthropic__ApiKey = "your-provider-key"
    dotnet run --project src/Hadm.WebApi

## Progressive web app

The Angular client includes a web app manifest, install icons, an install prompt for supported browsers, and an offline landing screen. The service worker is enabled in production builds and caches only app static assets; authenticated API requests and diary data are never cached. Install from the browser menu on Android/desktop, or use Safari's Share menu and choose Add to Home Screen on iPhone/iPad. Serve the production client over HTTPS (localhost is also a secure context) for service worker and install support. Angular development serve mode skips service worker registration.

## Projects

- `src/Hadm.Domain`: domain entities, enums, and red-flag rules.
- `src/Hadm.Application`: validated CQRS use cases and application contracts.
- `src/Hadm.Infrastructure`: SQL Server/Identity persistence, seeded data, JWT support, and PDF generation.
- `src/Hadm.WebApi`: authenticated HTTP API.
- `client`: standalone Angular application.

## Build

```sh
dotnet build HADM.sln
cd client && npm run build
```
