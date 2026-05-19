# Convention: No Hardcoded Data

**Decided:** 2026-05-19

## Rule

Never embed test or seed data directly inside code files or static JSON files.

## E2E Tests

All test data (orders, stock items, inbound POs, transfer orders, returns, etc.) must be created
by calling the live API endpoints before each test or test suite. Do not seed data by patching
in-memory state directly.

Typical pattern for Cypress / any e2e framework:

1. `beforeEach` / `before` — POST to the relevant API endpoint to create required data.
2. Store returned IDs / identifiers in test context for use in assertions.
3. `afterEach` / `after` — clean up via API (DELETE / cancel) if the endpoint supports it.

## Why

Hardcoded JSON data drifts from the real schema, hides integration bugs, and makes tests
unreliable as the API evolves. API-driven setup ensures tests always exercise the full stack.
