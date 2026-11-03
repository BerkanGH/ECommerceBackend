# ECommerce Backend

A .NET 10 Web API for a single-store catalog, customer carts, and orders.
This portfolio project focuses on domain rules, authorization, and consistent
stock updates rather than integrating real payment or shipping providers.

## Features

- Product-specific options and variants, with separate TRY and USD prices.
- Customer registration and sign-in using ASP.NET Core Identity bearer tokens.
- Customer-owned carts with server-calculated prices and a single currency.
- Transactional checkout, request idempotency, and one-time stock restoration on cancellation.
- Admin catalog management, product deactivation, and order status transitions.

## Stack and structure

ASP.NET Core 10, EF Core 10, PostgreSQL 18, Identity, MediatR, and xUnit.

| Project | Responsibility |
| --- | --- |
| Domain | Product, cart, and order rules |
| Application | Commands, queries, DTOs, and persistence interfaces |
| Infrastructure | EF Core mappings, PostgreSQL queries/locks, and Identity |
| Api | HTTP endpoints, authentication, and role checks |
| tests | Unit tests and PostgreSQL-backed API/persistence tests |

Commands and queries share one database. There is no event sourcing or separate
read database. See [architecture notes](docs/Architecture.md) for the design.

## Run locally

Requirements: a .NET 10 SDK and a running PostgreSQL server. Create a development
database named `ecommerce` first. The API never applies migrations automatically.

From the repository root, in PowerShell:

```powershell
dotnet restore ECommerce.slnx
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:ECommerce" "Host=localhost;Port=5432;Database=ecommerce;Username=your_user;Password=your_password" --project ECommerce.Api
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef migrations list --project ECommerce.Infrastructure --startup-project ECommerce.Api --configuration Release
dotnet ef database update --project ECommerce.Infrastructure --startup-project ECommerce.Api --configuration Release
dotnet run --project ECommerce.Api --configuration Release --launch-profile http
```

Replace the credentials locally; never commit them. User Secrets are for local
development, not a production secret store. Missing connection configuration
causes an explicit startup error rather than a silent fallback.

- API: http://localhost:5106
- OpenAPI JSON: http://localhost:5106/openapi/v1.json (Development only)
- Liveness: `/health/live`; readiness including migrations: `/health/ready`

For optional Docker PostgreSQL, use [the setup guide](docs/Operations.md).
Compose publishes PostgreSQL on **5433**, so its API connection string must use
5433, not the native installation's 5432. The API does not load `.env` automatically.

Create an admin with the explicit [bootstrap command](docs/Identity.md).
Public registration only creates customers. Request examples are in
`ECommerce.Api/Catalog.http`, `Identity.http`, `Cart.http`, and `Orders.http`.
For a complete customer walkthrough, see [placing an order](docs/Walkthrough.md).

## Tests

Unit tests do not need a database:

```powershell
dotnet test tests/ECommerce.UnitTests --configuration Release
```

To include PostgreSQL integration tests, provide a development/test connection
with permission to create schemas:

```powershell
$env:ECOMMERCE_TEST_CONNECTION = "Host=localhost;Port=5432;Database=ecommerce_tests;Username=your_user;Password=your_password"
dotnet test ECommerce.slnx --configuration Release
```

Each integration run creates and removes only its own randomly named schema.
It does not reset the database or delete existing application tables.
Without the variable, integration tests are reported as **skipped**, not passed.
See [test coverage and isolation](docs/Testing.md).

## CI and Docker

GitHub Actions is configured to build, check migrations, run both test projects
against PostgreSQL, and build/smoke-test the API container.
The Dockerfile uses a multi-stage build and a non-root runtime user.
See [current verification results](docs/Validation.md) for what was actually run.

## Scope and limitations

Checkout records an order; it does **not** collect money. Confirmation and shipping
statuses are manual. Real payment/shipping integrations, refunds, email verification,
password recovery, and MFA are not implemented.

Logout revokes refresh tokens; an existing access token can remain valid until
its 15-minute expiry. Deployment also needs TLS, secure Data Protection key storage,
monitoring, and backups. This is not presented as a production-ready store.
Review [MediatR's licensing](https://github.com/LuckyPennySoftware/MediatR) before production use.

For a short explanation of the main trade-offs in Turkish, see
[design discussion notes](docs/Interview.md).
