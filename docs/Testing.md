# Tests

The solution contains two xUnit projects. No mocking package, in-memory EF provider,
or Docker requirement is needed for the unit tests.

## Unit tests

```powershell
dotnet test tests/ECommerce.UnitTests --configuration Release
```

These cover option/value uniqueness, valid variant combinations, currency/price
rules, stock limits, cart quantities/currency, and order state transitions.

## PostgreSQL integration tests

Use a development/test database with CREATE SCHEMA permission. Prefer a separate
database such as ecommerce_tests; never use production credentials.

Create the database first, for example in pgAdmin. The fixture creates schemas,
not databases. On Windows, PostgreSQL's tool can also create it with a password prompt:

```powershell
& "C:\Program Files\PostgreSQL\18\bin\createdb.exe" --host=localhost --port=5432 --username=your_user --password ecommerce_tests
```

Do not recreate or reset an existing database.

```powershell
$env:ECOMMERCE_TEST_CONNECTION = "Host=localhost;Port=5432;Database=ecommerce_tests;Username=your_user;Password=your_password"
dotnet test ECommerce.slnx --configuration Release
Remove-Item Env:ECOMMERCE_TEST_CONNECTION
```

Use port 5433 if this is the Compose database. Environment values are process-local
and must not be copied into source files. Stop an API using the same build output
before running a build/test command on Windows.

Without the variable, PostgreSQL tests are explicitly skipped. A configured but
unreachable database fails the suite; it is not silently skipped.

## Isolation

PostgresFixture creates a schema named ecommerce_test_<random GUID>, sets the
connection search_path to only that schema, and applies the real migrations.
The migration history table is also inside the schema. No EnsureDeleted,
TRUNCATE, database reset, or public-table cleanup is used.

The fixture removes only the schema it created, with a generated-name guard,
and restores its test-process configuration even if the host fails.
Existing application tables and data are not reset. The connection still needs
permission to create/drop its own schema; insufficient permission is a test setup failure.

WebApplicationFactory runs the real middleware, controllers, Identity, MediatR
handlers, repositories, and PostgreSQL queries. Test tokens use an ephemeral
Data Protection provider, separate from the local application's key store.

Tests run serially at collection level, but selected tests issue actual simultaneous
HTTP requests to reproduce checkout and cancellation races.

## Covered integration scenarios

- Authentication/role boundaries and customer-owned carts/orders.
- Correct TRY/USD pricing; no fallback when a currency price is absent.
- Atomic checkout, stale-price confirmation, and historical snapshots.
- Same-key retries and concurrent duplicate checkout.
- Two customers competing for the final stock unit.
- Concurrent cancellation returning stock once.
- Inactive products and cancellation of their historical orders.
- Product/cart optimistic concurrency and deleted cart items.
- Revision updates only for affected aggregates.
- PostgreSQL unique SKU constraints, migrations, readiness, and catalog reads.

The suite does not cover every endpoint, email/MFA, payments, container networking,
or load/performance characteristics. Passing these tests is not a security certification.

## CI

The workflow supplies both ConnectionStrings__ECommerce and ECOMMERCE_TEST_CONNECTION.
It builds first, validates/applies migrations, then runs dotnet test --no-build.
Integration tests are therefore enabled in CI. Docker build and container smoke
steps are configured as separate checks, not implied by a successful unit test run.
