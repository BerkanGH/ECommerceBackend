# Verification record

## Local audit

- PostgreSQL 18 was running on port 5432; Docker's Linux engine was unavailable.
- No User Secrets connection or connection environment variable was initially set.
- Npgsql confirmed that the fallback connection lacked the password required by the server.
- The supplied User Secrets connection authenticated successfully.
- The target public schema was empty. The five migration scripts were reviewed
  for destructive statements and applied explicitly, not on API startup.
- The current Release API returned 200 from /health/live, /health/ready,
  and /api/products. The product count was zero; no sample products were inserted.
- The process initially on port 5106 was stale and returned 404 for /health/ready.
  Updated output was verified on a separate port without assuming that old process reloaded.

## Automated checks

The committed suite replaces the earlier ad-hoc assertion reports:

- 21 xUnit unit tests passed.
- 16 PostgreSQL integration tests passed.
- No tests were skipped when the test connection was supplied.
- Tests used isolated generated schemas; no application database was reset.
- The configured native PostgreSQL server was also tested. Afterwards, public
  product/user/order counts remained zero and no generated test schema remained.

These numbers describe the recorded local run. Use dotnet test to obtain current
results rather than treating this file as a permanent test badge.

## CI and Docker

The original remote workflow at commit f96f252 completed successfully:
[GitHub Actions run](https://github.com/BerkanGH/ECommerceBackend/actions/runs/41580742220).
That workflow did not run the new tests.

The updated workflow includes automated tests and container validation. It must
be pushed and run before its remote status can be reported as successful.
Compose configuration was checked: host 5433 maps to container 5432, bound to loopback.
Docker image build/start was not run locally because the daemon was unavailable.

This record is not a performance, load, or production-security claim.
