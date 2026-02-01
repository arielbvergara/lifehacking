# 18. Replace PostgreSQL Persistence with Firebase Database

## Status
- **Status**: Accepted
- **Date**: 2026-02-01
- **Related issue**: #2

## Context

The `lifehacking` backend currently uses Entity Framework Core with PostgreSQL as the primary persistence layer:

- `Infrastructure/Data/AppDbContext` models the relational schema.
- `Infrastructure/Data/AppDbContextFactory` configures the EF Core providers, including an in-memory database for tests and PostgreSQL for the real database.
- `WebAPI/Configuration/DatabaseConfiguration` wires the database selection based on configuration:
  - When `UseInMemoryDB` is `true` or the environment is `Testing`, the in-memory database is used.
  - Otherwise, PostgreSQL is configured using `ConnectionStrings:DbContext`.

Authentication and identity are already integrated with Firebase (see ADR-004, ADR-007, ADR-008, ADR-014, ADR-016, and ADR-017), but persistence for users and related entities still depends on PostgreSQL.

This project is not yet in production, so there is **no existing production data** that needs to be migrated. This allows us to change the persistence technology without designing or executing a production data migration strategy as part of this work.

We want to simplify the stack, align persistence with the existing Firebase-based identity model, and enable local development and test workflows that do not require a running PostgreSQL instance.

## Decision

We will replace PostgreSQL-backed persistence with a Firebase-backed database as the primary datastore for the `lifehacking` backend, while preserving the existing Clean Architecture boundaries:

1. **Use Firebase as the primary datastore instead of PostgreSQL**
   - The default persistence for application entities (starting with `User` and related aggregates) will be a Firebase database (Firestore or Realtime Database, depending on the existing Firebase configuration for this project).
   - PostgreSQL will no longer be required for normal development and test workflows.

2. **Keep the Domain layer persistence-agnostic**
   - The `Domain` project remains unchanged and continues to have no dependency on EF Core, PostgreSQL, or Firebase.
   - Domain entities, value objects, and `Result<T, AppException>` primitives remain the single source of truth for business rules.

3. **Introduce Firebase-backed infrastructure implementations**
   - Existing repository interfaces in `Application.Interfaces` (e.g., `IUserRepository`) will be implemented in `Infrastructure` using Firebase instead of EF Core / PostgreSQL.
   - A dedicated Firebase access abstraction will be introduced in `Infrastructure` (and/or via an interface in `Application.Interfaces`) to encapsulate:
     - Connection to the Firebase project/database.
     - CRUD operations and queries for domain entities.
     - Mapping between domain models and Firebase document/record representations.

4. **Retain the in-memory provider for tests and specific scenarios**
   - The existing in-memory EF Core configuration remains available for testing and the `Testing` environment.
   - Configuration will allow selecting between:
     - In-memory database (for tests and specific dev workflows).
     - Firebase (the default for development and future production-like environments).

5. **Update WebAPI configuration to select Firebase via configuration**
   - `WebAPI/Configuration/DatabaseConfiguration` will be updated so that:
     - `UseInMemoryDB = true` (or `Testing` environment) continues to select the in-memory database.
     - Otherwise, Firebase becomes the default persistence provider, configured via a new `Firebase` configuration section (e.g., project ID, database URL, credentials location).
   - No PostgreSQL-specific configuration will be required for the standard development path once this change is complete.

6. **Align admin bootstrap and seeding with Firebase persistence**
   - Existing admin bootstrap behavior (see ADR-007, ADR-017) will be updated so any admin seeding or ensure flows persist users into Firebase instead of PostgreSQL.
   - Seeding remains idempotent and is safe to run on startup, relying on existing application use cases where possible.

No production data migration plan is included in this decision. If and when this system is promoted to a production environment with real user data, a separate ADR and implementation work will define any required migration or backfill strategy.

## Consequences

### Positive

- **Simplified stack for development**
  - Developers can run the backend using Firebase (and/or Firebase emulators) without needing a local PostgreSQL instance.
  - The persistence and identity stacks are aligned around Firebase, reducing the cognitive load and configuration surface.

- **Stronger alignment with existing identity model**
  - Since authentication and admin bootstrap already use Firebase, storing user records in Firebase reduces the impedance mismatch between identity data and application data.

- **Clean Architecture preserved**
  - The `Domain` project remains persistence-agnostic.
  - Application use cases continue to depend on interfaces, with `Infrastructure` providing Firebase-backed implementations.

- **Testability**
  - The existing in-memory database path remains available for fast, deterministic tests.
  - Additional integration tests can be added against Firebase (preferably via emulators) to validate real-world persistence behavior.

### Negative / Trade-offs

- **Vendor and technology lock-in**
  - Moving persistence to Firebase increases coupling to Firebase-specific capabilities (document model, querying, indexing, security rules).
  - Future moves away from Firebase will require another translation layer or migration effort.

- **Data modeling and querying constraints**
  - Relational patterns (joins, complex transactions) supported by PostgreSQL must be re-modeled to fit Firebase's document-oriented model.
  - Some queries may require denormalization or additional collections to remain efficient.

- **Operational complexity around Firebase**
  - Local development and CI will need Firebase emulator support (or a dedicated test project) and associated configuration.
  - Security rules, indexes, and quotas must be managed carefully to avoid runtime issues.

## Implementation Notes

- **Firebase configuration**
  - Introduce a `Firebase` configuration section for the WebAPI project, with keys such as:
    - `Firebase:ProjectId`
    - `Firebase:DatabaseUrl`
    - `Firebase:CredentialsPath` or similar.
  - Secrets and credentials will be provided via environment variables, secret stores, or external configuration; they must not be committed to source control.

- **Infrastructure mapping**
  - Repositories in `Infrastructure.Repositories` will:
    - Map domain entities (`Domain.Entities.User` and others) to Firebase documents.
    - Handle conversions of IDs, timestamps, and soft-delete flags consistently with existing domain semantics (see ADR-006 for user lifecycle).

- **Testing**
  - Existing unit and application tests continue to use the in-memory database and current patterns.
  - New integration tests will target the Firebase-backed repositories, preferably using Firebase emulators to avoid hitting live services in CI.

- **Configuration toggles**
  - During implementation, a transitional configuration may temporarily support both PostgreSQL and Firebase; however, once Firebase-backed persistence is validated, PostgreSQL support will be retired for normal development workflows.

## References

- ADR-004: Firebase Authentication and Identity Provider Abstraction.
- ADR-006: User Role and Soft Delete Lifecycle.
- ADR-007: Admin Bootstrap and Firebase Admin Integration.
- ADR-008: Admin-Only User Id/Email Endpoints.
- ADR-010: Hardened Production Configuration.
- ADR-014: Security Design and Threat Model for User Endpoints.
- ADR-017: Admin User Creation Endpoint and Shared Use Case.
- GitHub issue: #2 – Replace PostgreSQL persistence with Firebase database.
