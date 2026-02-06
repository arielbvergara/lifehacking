# 19. Firestore Emulator Test Infrastructure and Property-Based Testing

## Status
- **Status**: Accepted
- **Date**: 2026-02-06
- **Related issue**: N/A

## Context

Following the migration from PostgreSQL to Firebase Firestore (ADR-018), the test infrastructure initially relied on EF Core's in-memory database provider for testing. This approach had several critical limitations:

- **Impedance Mismatch**: The in-memory database did not accurately represent Firestore's document-oriented model, query capabilities, or constraints, leading to tests that passed locally but could fail against real Firestore.
- **Test Isolation Issues**: Tests shared collections in the Firestore emulator, causing data contamination and unreliable test results, especially when running tests in parallel.
- **Incomplete Soft Delete Support**: While the User entity supported soft deletion, Tip and Category entities lacked this functionality, creating inconsistent behavior across the domain model.
- **Unrealistic Test Data**: Search and query tests used simplistic test data with brittle assertions, making tests fragile and less meaningful.

The existing test infrastructure needed to evolve to:
1. Test against the actual Firestore emulator rather than an in-memory abstraction
2. Provide proper test isolation for parallel execution
3. Establish consistent soft delete patterns across all entities
4. Introduce property-based testing to verify universal correctness properties

## Decision

We will replace the EF Core in-memory test infrastructure with a comprehensive Firestore emulator-based testing approach that includes per-test collection isolation, consistent soft delete support, and property-based testing:

### 1. Replace In-Memory Database with Firestore Emulator

- **Remove EF Core in-memory database dependency** from all test projects (Application.Tests, Infrastructure.Tests, WebAPI.Tests).
- **Use the Firestore emulator** (running on 127.0.0.1:8080) as the exclusive test database for all integration and repository tests.
- **Maintain FirestoreTestBase** as the common base class for tests, providing emulator connection setup and repository initialization.

### 2. Implement Per-Test Collection Namespacing

- **Introduce ICollectionNameProvider interface** to abstract collection name generation:
  - `TestCollectionNameProvider`: Generates unique collection names per test instance using GUID-based suffixes (e.g., `users_abc12345`).
  - `ProductionCollectionNameProvider`: Returns base collection names without modification for production use.
- **Update all data stores** (FirestoreUserDataStore, FirestoreTipDataStore, FirestoreCategoryDataStore) to accept `ICollectionNameProvider` as a constructor dependency.
- **Generate unique collection names** for each test instance, ensuring complete isolation between concurrent test executions.
- **Eliminate cleanup logic**: Per-test collections naturally isolate data, removing the need for explicit cleanup between tests.

### 3. Extend Soft Delete Support to All Entities

- **Add soft delete properties to Tip entity**:
  - `IsDeleted` (bool, default false)
  - `DeletedAt` (DateTime?, nullable)
  - `MarkDeleted()` method (idempotent)
- **Add soft delete properties to Category entity**:
  - `IsDeleted` (bool, default false)
  - `DeletedAt` (DateTime?, nullable)
  - `MarkDeleted()` method (idempotent)
- **Update Firestore documents** (TipDocument, CategoryDocument) to include `isDeleted` and `deletedAt` fields.
- **Implement soft delete filtering at the data store layer**:
  - All query methods apply `WhereEqualTo("isDeleted", false)` filter at the Firestore query level.
  - GetById methods return null for soft-deleted entities.
  - Provide explicit methods (GetAllIncludingDeletedAsync) for retrieving deleted entities when needed.
- **Maintain consistency** with existing User entity soft delete behavior.

### 4. Introduce Property-Based Testing

- **Adopt FsCheck** as the property-based testing library for C# projects.
- **Define 12 correctness properties** covering:
  - Collection name uniqueness and format
  - Soft delete initial state, idempotence, and persistence
  - Repository filtering consistency across all entities
  - Test data factory validity
- **Run property tests with minimum 100 iterations** to verify properties across randomized inputs.
- **Tag each property test** with references to the design document properties using the format: `// Feature: firestore-test-infrastructure-improvements, Property {number}: {property_text}`.
- **Complement unit tests with property tests**: Unit tests verify specific examples and edge cases, while property tests verify universal correctness across all inputs.

### 5. Establish Realistic Test Data Patterns

- **Create TestDataFactory class** to centralize test data creation:
  - Provides factory methods for creating User, Tip, and Category entities with realistic data.
  - Establishes proper entity relationships (tips reference categories).
  - Generates varied content for search testing scenarios.
- **Use flexible assertions**:
  - "At least N results" assertions for search tests with varied data.
  - Exact match assertions for specific data retrieval tests.
  - Explicit documentation of assertion strategies in test comments.

### 6. Support Parallel Test Execution

- **Leverage xUnit's parallel execution** capabilities with confidence that collection namespacing prevents data contamination.
- **Verify parallel execution** through dedicated integration tests that run concurrent operations.
- **Eliminate shared state** in test infrastructure to prevent race conditions.

### 7. CI/CD Integration

- **Start Firestore emulator** before running tests in CI/CD pipelines (GitHub Actions).
- **Run tests in parallel** to verify isolation and reduce execution time.
- **Fail builds on test failures** to maintain quality gates.
- **Provide helper scripts** (`scripts/start-emulator.sh`, `scripts/test-with-emulator.sh`) for local development and CI use.

## Consequences

### Positive

- **Higher Test Fidelity**
  - Tests run against the actual Firestore emulator, accurately representing production behavior including query semantics, indexing, and document model constraints.
  - Eliminates the impedance mismatch between in-memory databases and Firestore.

- **Reliable Parallel Test Execution**
  - Per-test collection namespacing ensures complete isolation between tests.
  - Tests can run in parallel without data contamination or race conditions.
  - Faster test execution through parallelization.

- **Consistent Domain Model**
  - All entities (User, Tip, Category) now have uniform soft delete support.
  - Predictable deletion semantics across the entire domain.
  - Simplified reasoning about entity lifecycle.

- **Stronger Correctness Guarantees**
  - Property-based testing verifies universal properties across randomized inputs.
  - Catches edge cases and corner cases that unit tests might miss.
  - Provides mathematical confidence in system behavior.

- **Improved Test Maintainability**
  - TestDataFactory centralizes test data creation, reducing duplication.
  - Realistic test data makes tests more meaningful and easier to understand.
  - Clear separation between unit tests (specific examples) and property tests (universal properties).

- **Clean Architecture Preserved**
  - Domain entities remain persistence-agnostic.
  - Collection name provider abstraction keeps infrastructure concerns isolated.
  - Repository interfaces unchanged; filtering happens transparently at the data store layer.

### Negative / Trade-offs

- **Firestore Emulator Dependency**
  - Tests require the Firestore emulator to be running (local development and CI).
  - Adds setup complexity compared to pure in-memory tests.
  - Emulator must be started before running tests, requiring additional scripts and documentation.

- **Slower Test Execution (Compared to In-Memory)**
  - Firestore emulator operations are slower than in-memory database operations.
  - Network overhead (even to localhost) adds latency.
  - Mitigated by parallel execution and the accuracy benefits.

- **Property-Based Testing Learning Curve**
  - Developers unfamiliar with property-based testing need to learn FsCheck and property-oriented thinking.
  - Writing effective generators and properties requires different skills than traditional unit testing.
  - Mitigated by clear documentation and examples in the codebase.

- **Collection Namespace Proliferation**
  - Each test creates new collections in the emulator, leading to many collections over time.
  - Emulator cleanup may be needed periodically during development.
  - Mitigated by emulator reset scripts and the fact that collections are isolated per test run.

- **Soft Delete Filtering Overhead**
  - Every query includes an additional `WhereEqualTo("isDeleted", false)` filter.
  - Slight performance overhead compared to queries without filtering.
  - Mitigated by Firestore's efficient query execution and the correctness benefits.

- **Increased Test Execution Time for Property Tests**
  - Property tests run 100+ iterations per test, increasing total test time.
  - Mitigated by the comprehensive coverage and bug detection capabilities.

## Implementation Notes

### Firestore Emulator Setup

- **Local Development**:
  - Use `scripts/start-emulator.sh` to start the emulator on 127.0.0.1:8080.
  - Use `scripts/test-with-emulator.sh` to run tests with the emulator.
  - Use `scripts/stop-emulator.sh` to stop the emulator.
  - Use `scripts/reset-emulator.sh` to clear emulator data.

- **CI/CD (GitHub Actions)**:
  - Install Firebase CLI in the CI environment.
  - Start the emulator before running tests.
  - Configure environment variables to point to the emulator (FIRESTORE_EMULATOR_HOST=127.0.0.1:8080).
  - Stop the emulator after tests complete.

### Collection Name Provider Pattern

- **Test Environment**: Use `TestCollectionNameProvider` which generates unique suffixes per test instance.
- **Production Environment**: Use `ProductionCollectionNameProvider` which returns base collection names unchanged.
- **Dependency Injection**: Register the appropriate provider based on environment configuration.

### Soft Delete Implementation

- **Domain Layer**: Entities have `IsDeleted`, `DeletedAt`, and `MarkDeleted()` method.
- **Infrastructure Layer**:
  - Documents include `isDeleted` and `deletedAt` fields.
  - Data stores apply `WhereEqualTo("isDeleted", false)` filter in all query methods.
  - GetById methods check `IsDeleted` and return null if true.
- **Repository Layer**: No changes required; filtering is transparent.

### Property-Based Testing

- **Library**: FsCheck (installed via NuGet in test projects).
- **Test Organization**: Property tests are separate from unit tests, clearly marked with property tags.
- **Generators**: Custom generators for domain entities (User, Tip, Category) with realistic constraints.
- **Iterations**: Minimum 100 iterations per property test (configurable via FsCheck attributes).
- **Failure Reporting**: FsCheck provides shrinking to find minimal failing examples when properties fail.

### Test Data Factory

- **Location**: `lifehacking/Tests/Infrastructure.Tests/TestDataFactory.cs` (shared across test projects).
- **Methods**:
  - `CreateUser()`: Creates a user with realistic email, name, and external auth ID.
  - `CreateCategory()`: Creates a category with a realistic name.
  - `CreateTip()`: Creates a tip with realistic title, description, steps, and tags, linked to a category.
  - `CreateTipsForSearch()`: Creates multiple tips with varied content for search testing.
- **Usage**: All tests should use TestDataFactory instead of manually constructing entities.

### Migration from In-Memory Database

- **Remove EF Core Dependencies**: Remove `Microsoft.EntityFrameworkCore.InMemory` package references from test projects.
- **Update FirestoreTestBase**: Remove in-memory database setup, focus exclusively on Firestore emulator connection.
- **Update Existing Tests**: Ensure all tests use FirestoreTestBase and TestDataFactory.
- **Verify Test Isolation**: Run tests in parallel to confirm no data contamination occurs.

## References

- ADR-018: Replace PostgreSQL Persistence with Firebase Database.
- ADR-006: User Role and Soft Delete Lifecycle.
- FsCheck Documentation: https://fscheck.github.io/FsCheck/
- Firebase Emulator Suite: https://firebase.google.com/docs/emulator-suite
