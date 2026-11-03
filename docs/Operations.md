# Local development and database troubleshooting

## Choose a PostgreSQL instance

For this Windows setup, the installed PostgreSQL 18 service is the verified route:
it listens on localhost:5432. Docker Desktop's engine was unavailable during the
audit. Docker is optional, not a requirement for running the application or tests.

```powershell
Get-Service '*postgres*'
Test-NetConnection localhost -Port 5432
```

A running service or an open port does not prove that the application can
authenticate. Verify the database and role with a password prompt:

```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" --host=localhost --port=5432 --username=your_user --dbname=ecommerce --password -c "SELECT current_database(), current_user;"
```

Do not disable authentication or change pg_hba.conf to work around a missing password.

## Configure and migrate

Use the README's User Secrets command, then explicitly select Development so
EF tools also load the secrets. Review the target database and pending migrations:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet tool restore
dotnet ef migrations list --project ECommerce.Infrastructure --startup-project ECommerce.Api --configuration Release
dotnet ef migrations script --idempotent --project ECommerce.Infrastructure --startup-project ECommerce.Api --configuration Release --output .artifacts/migrations.sql
dotnet ef database update --project ECommerce.Infrastructure --startup-project ECommerce.Api --configuration Release
```

Review SQL before applying it to an existing database. The API does not migrate
on startup. Do not use database reset commands to fix connection failures.

User Secrets live outside Git and are not encrypted. Avoid putting real values
in README, appsettings, .http files, screenshots, or shell transcripts.
An environment variable named ConnectionStrings__ECommerce overrides User Secrets.

## Optional Docker database

```powershell
Copy-Item .env.example .env
```

Replace the example password in .env locally, then:

```powershell
docker info
docker compose config --quiet
docker compose up -d postgres
docker compose ps
Test-NetConnection localhost -Port 5433
```

The defaults are database ecommerce, user ecommerce, and host port 5433. Configure
the API separately with Host=localhost;Port=5433 and the matching credentials.
Compose reads .env; ASP.NET Core does not. Never paste the real password into Git.

The named volume persists data. Do not use docker compose down -v to troubleshoot.
Changing POSTGRES_PASSWORD in .env does not change an existing database user's password.

To build the API image when the Docker engine is available:

```powershell
docker build -t ecommerce-api:local .
```

The container needs ConnectionStrings__ECommerce at runtime. localhost inside a
container is not the host computer; use the appropriate Docker network address.
Production also requires TLS/reverse-proxy settings and persistent protected
Data Protection keys. Docker image build/run was not verified locally during this audit.

## Diagnosing a 503

1. Confirm which instance and port you intend to use: native 5432 or Compose 5433.
2. Check the API's effective configuration without printing its password.
3. Test authentication and database existence with psql.
4. Inspect pending migrations, then apply them deliberately.
5. Restart an old API process after changing startup-bound configuration.
6. Request /health/live, /health/ready, and /api/products.

The original failure here was a missing connection configuration: the API silently
used a postgres connection without a password, but the server required SCRAM.
The fallback has been removed. A later configured connection authenticated
successfully; five pending migrations were reviewed/applied to an empty database.
The updated API returned 200 from all three endpoints without adding sample products.

Readiness returns plain text Healthy (200) or Unhealthy (503), not JSON.
Stop the relevant API instance before rebuilding its output files on Windows.
An old Visual Studio process can still serve old code even after a Release build succeeds.
