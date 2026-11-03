# Architecture and reliability audit

## Main findings and changes

| Finding | Response |
| --- | --- |
| Missing database credentials were hidden by a fallback connection | Require explicit configuration; verify service, authentication, schema, and migrations separately |
| A running API process was serving older code | Verify a current Release instance separately; restart the stale process deliberately |
| No committed behavioral tests | Add xUnit unit tests and PostgreSQL-backed API/persistence tests |
| UnitOfWork updated revisions for unchanged, unrelated roots | Collect changed entities and foreign keys; update only affected root revisions |
| README and validation notes repeated unrepeatable temporary reports | Shorten README, add reproducible test instructions, and distinguish local checks from remote CI |

The original public schema had no tables. Its five migrations were reviewed and
applied without dropping or resetting user data. Tests do not use public-table resets.

## Decisions kept

- Four projects keep domain rules independent of HTTP and EF Core.
- MediatR already gives each use case a clear handler; replacing it would add
  migration work without fixing the reported reliability problems.
- Command/query separation uses one database. No additional CQRS infrastructure is needed.
- Purpose-specific repositories keep PostgreSQL locks and SQL out of Application.
- UnitOfWork is retained for explicit multi-aggregate transactions and revision handling.
  EF Core already tracks changes; this interface is not a second change tracker.
- Existing advisory/row locks and idempotency checks remain in place and are
  exercised against PostgreSQL, not replaced by mocks.

Revision mapping is intentionally explicit for the existing entity types.
Adding a new aggregate child requires updating this mapping and its tests.
Deleted cart items must still advance the parent revision, even after leaving
the in-memory collection.

## Remaining limitations

Logout invalidates refresh tokens, not already-issued access tokens immediately.
Access tokens can remain valid for 15 minutes. Email confirmation/recovery and
MFA are absent; these are documented limitations, not completed security features.

Checkout can encounter a deadlock/concurrency conflict; 409 and idempotent retry
are part of the contract. Tests cover specific races, not all interleavings or load.

Docker's daemon was unavailable locally. Compose syntax/ports were checked,
but image/container success must be verified by running the updated CI or Docker locally.
TLS, proxy trust, key persistence, monitoring, and backups remain deployment work.

## Useful topics to explain in an interview

1. Why a cart is not a stock reservation.
2. Why an order stores a price/name/address snapshot.
3. Why checking a SKU with SELECT is not a uniqueness guarantee.
4. How row locks differ from an optimistic Revision token.
5. Why idempotency is scoped to a customer and request payload.
6. How the integration fixture prevents changes to existing application tables.

## Changed files and reasons

| File | Reason |
| --- | --- |
| ECommerce.Api/Program.cs | Remove the passwordless fallback and expose the standard test entry point |
| ECommerce.Infrastructure/Persistence/ECommerceDbContext.cs | Advance revisions only for aggregates affected by tracked changes |
| ECommerce.slnx | Include both test projects in normal solution builds |
| tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj | Add xUnit/VSTest without a mocking framework |
| tests/ECommerce.UnitTests/ProductTests.cs | Check combinations, prices, SKU-related rules, and inventory boundaries |
| tests/ECommerce.UnitTests/CartTests.cs | Check quantities, merging, line limits, and currency changes |
| tests/ECommerce.UnitTests/OrderTests.cs | Check snapshots, totals, cancellation, and state transitions |
| tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj | Add the real API test host and project references |
| tests/ECommerce.IntegrationTests/PostgresFixture.cs | Isolate migrations/data/keys and clean up only the generated schema |
| tests/ECommerce.IntegrationTests/CheckoutTests.cs | Exercise real checkout, ownership, pricing, and concurrent requests |
| tests/ECommerce.IntegrationTests/PersistenceTests.cs | Check revisions, SQL constraints, cart deletion, health, and catalog reads |
| .github/workflows/ci.yml | Enable tests and explicit Docker build/start checks |
| .gitignore | Exclude generated test results |
| README.md | Provide a concise English entry point and reproducible commands |
| docs/Operations.md | Separate native 5432 from Compose 5433 and document the diagnosed failure |
| docs/Testing.md | Explain test prerequisites, isolation, coverage, and skip behavior |
| docs/Validation.md | Replace ad-hoc assertion totals with current, attributable results |
| docs/MVP-Tasarim.md | Keep scope without a repeated development chronicle |
| docs/Cart.md | Link to committed tests instead of obsolete temporary-test claims |
| docs/Orders.md | Link the order rules to reproducible tests |
| docs/Identity.md | Correct test status without implying missing security features were added |
| docs/Audit.md | Record findings, retained architecture, and remaining limits |
