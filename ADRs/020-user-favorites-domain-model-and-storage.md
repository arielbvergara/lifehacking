# 20. User Favorites Domain Model and Storage

## Status
- **Status**: Accepted
- **Date**: 2026-02-06
- **Related issue**: [#10](https://github.com/arielbvergara/lifehacking/issues/10)

## Context

The lifehacking application allows users to browse tips across various categories. As the tip catalog grows, users need a way to bookmark tips they find particularly useful for quick reference later. This requires introducing a favorites feature that allows users to:

- Add tips to a personal favorites list
- Remove tips from their favorites
- View and search their favorited tips with the same filtering capabilities as the main tips search

The favorites feature must integrate cleanly with the existing clean architecture, maintain domain model integrity, and leverage Firestore's document model efficiently.

### Key Design Considerations

1. **Domain Model Structure**: Should favorites be a property on the User entity or a separate entity?
2. **Storage Strategy**: How should favorites be stored in Firestore to optimize for queries and prevent duplicates?
3. **Deletion Semantics**: Should favorites use soft-delete like other entities, or true deletion?
4. **Search Capabilities**: How should favorites search integrate with existing tip search functionality?
5. **Concurrency**: How do we prevent race conditions when adding/removing favorites?

## Decision

We will implement favorites as a separate `UserFavorites` entity with Firestore composite document IDs for natural deduplication and efficient queries.

### 1. Separate UserFavorites Entity

- **Create UserFavorites domain entity** as a separate entity (not a collection property on User):
  - Properties: `UserId`, `TipId`, `AddedAt` (DateTime)
  - All properties are immutable after creation
  - Factory methods: `Create(UserId, TipId)` and `FromPersistence(...)`
  - No soft-delete support (true deletion only)

- **Rationale**:
  - Keeps User entity lean and focused on user identity/authentication concerns
  - Allows favorites to scale independently without bloating User documents
  - Enables efficient querying and filtering of favorites
  - Provides clear separation of concerns in the domain model

### 2. Firestore Composite Document ID Strategy

- **Use composite document IDs** in the format `{userId}_{tipId}`:
  - Collection: `favorites`
  - Document ID: `{userId}_{tipId}` (e.g., `abc123_def456`)
  - Document fields: `userId`, `tipId`, `addedAt`

- **Rationale**:
  - **Natural Deduplication**: Firestore document IDs are unique, preventing duplicate favorites automatically
  - **Efficient Lookups**: Direct document access via composite key (O(1) operation)
  - **Simple Deletion**: Single document delete operation, no queries needed
  - **Atomic Operations**: Add/remove operations are atomic at the document level
  - **No Indexes Required**: Composite key eliminates need for compound indexes on userId+tipId

- **Alternative Considered**: Subcollection pattern (`users/{userId}/favorites/{tipId}`)
  - Rejected because composite keys are simpler for this use case
  - Subcollections add nesting complexity without clear benefits
  - Composite keys allow easier cross-user queries if needed in the future

### 3. True Deletion (No Soft-Delete)

- **Favorites use permanent deletion** when removed:
  - `RemoveAsync` performs Firestore document deletion
  - No `IsDeleted` or `DeletedAt` fields
  - Removed favorites leave no trace in the database

- **Rationale**:
  - Favorites are user preferences, not business-critical data
  - No audit trail or recovery requirements for favorites
  - Simpler implementation without soft-delete filtering
  - Reduces storage costs (no accumulation of deleted favorites)
  - Consistent with user expectations (removed = gone)

### 4. Repository Pattern with Tip Coordination

- **IFavoritesRepository interface** provides high-level operations:
  - `GetByUserAndTipAsync`: Retrieve specific favorite
  - `AddAsync`: Add new favorite
  - `RemoveAsync`: Remove favorite
  - `SearchUserFavoritesAsync`: Search with filtering/sorting/pagination
  - `ExistsAsync`: Check if tip is favorited

- **FavoritesRepository implementation** coordinates between data stores:
  - Delegates to `IFirestoreFavoriteDataStore` for favorites operations
  - Delegates to `IFirestoreTipDataStore` to fetch actual tip details
  - Applies filtering (search term, category, tags) on fetched tips
  - Applies sorting (title, createdAt, updatedAt) on fetched tips
  - Returns full `Tip` entities, not just IDs

- **Rationale**:
  - Repository abstracts coordination complexity from use cases
  - Use cases work with domain entities, not Firestore documents
  - Filtering at repository level allows rich search capabilities
  - Maintains clean architecture boundaries

### 5. Application Layer Use Cases

- **AddFavoriteUseCase**:
  - Validates user exists
  - Validates tip exists
  - Checks for duplicate (returns `ConflictException` if exists)
  - Creates and persists `UserFavorites` entity
  - Returns `FavoriteResponse` with full tip details

- **RemoveFavoriteUseCase**:
  - Validates user exists
  - Checks favorite exists (returns `NotFoundException` if not)
  - Removes favorite from repository
  - Returns boolean success indicator

- **SearchUserFavoritesUseCase**:
  - Validates user exists
  - Delegates to repository for search
  - Batch-fetches category names (avoids N+1 queries)
  - Returns `PagedFavoritesResponse` with pagination metadata

- **Error Handling**:
  - `ConflictException` (409): Duplicate favorite
  - `NotFoundException` (404): User not found, tip not found, favorite not found
  - `InfraException` (500): Infrastructure failures

### 6. DTOs and Extension Methods

- **Request DTOs**:
  - `AddFavoriteRequest(UserId, TipId)`
  - `RemoveFavoriteRequest(UserId, TipId)`
  - `SearchUserFavoritesRequest(UserId, TipQueryCriteria)`

- **Response DTOs**:
  - `FavoriteResponse(TipId, AddedAt, TipDetailResponse)`
  - `PagedFavoritesResponse(Favorites, PaginationMetadata)`

- **Extension Methods**:
  - `ToFavoriteResponse(UserFavorites, Tip, categoryName)`: Converts entities to DTO

- **Rationale**:
  - Reuses existing `TipQueryCriteria` for consistent search behavior
  - Reuses existing `PaginationMetadata` for consistent pagination
  - Provides full tip details in responses (not just IDs)

### 7. Firestore Data Store Layer

- **FavoriteDocument**:
  - Properties: `UserId` (string), `TipId` (string), `AddedAt` (DateTime)
  - Firestore attributes: `[FirestoreData]`, `[FirestoreProperty]`
  - Conversion methods: `ToEntity()`, `FromEntity(UserFavorites)`
  - Helper: `CreateDocumentId(UserId, TipId)` for composite key generation

- **FirestoreFavoriteDataStore**:
  - Implements `IFirestoreFavoriteDataStore`
  - Uses `ICollectionNameProvider` for test isolation
  - `GetByCompositeKeyAsync`: Direct document access
  - `AddAsync`: SetAsync with composite document ID
  - `RemoveAsync`: DeleteAsync with existence check
  - `SearchAsync`: Query by userId, sort by addedAt, paginate
  - `ExistsAsync`: Checks document existence

- **Rationale**:
  - Follows existing Firestore data store patterns
  - Uses collection name provider for test isolation
  - Leverages Firestore's document model efficiently

## Consequences

### Positive

- **Clean Domain Model**
  - UserFavorites is a first-class domain entity with clear semantics
  - No infrastructure dependencies in domain layer
  - Immutable after creation (append-only model)

- **Efficient Firestore Usage**
  - Composite document IDs provide O(1) lookups
  - Natural deduplication without additional queries
  - Atomic add/remove operations
  - No compound indexes required

- **Consistent Error Handling**
  - Follows existing Result<T, AppException> pattern
  - Appropriate HTTP status codes (409 Conflict, 404 Not Found)
  - Clear error messages for debugging

- **Rich Search Capabilities**
  - Reuses existing TipQueryCriteria for consistency
  - Supports search by term, category, tags
  - Supports sorting by multiple fields
  - Pagination support

- **Test Isolation**
  - Uses ICollectionNameProvider for per-test collections
  - Follows existing test infrastructure patterns
  - Property-based testing support

- **Scalability**
  - Favorites scale independently of User documents
  - No document size limits (User documents stay small)
  - Efficient queries even with many favorites

### Negative / Trade-offs

- **Multiple Document Fetches for Search**
  - SearchUserFavoritesAsync fetches favorites, then fetches each tip individually
  - N+1 query pattern (1 favorites query + N tip queries)
  - Mitigated by pagination limiting N
  - Alternative (denormalizing tip data into favorites) rejected due to data consistency concerns

- **No Favorites History**
  - True deletion means no audit trail of removed favorites
  - Cannot recover accidentally removed favorites
  - Cannot analyze favorite patterns over time
  - Acceptable trade-off for this use case (user preferences, not business data)

- **Filtering After Fetch**
  - Search term, category, and tag filtering happens in-memory after fetching tips
  - Cannot leverage Firestore indexes for these filters
  - Mitigated by pagination limiting data fetched
  - Alternative (storing denormalized tip data) rejected due to consistency concerns

- **Composite Key Parsing**
  - Document IDs must be parsed to extract userId and tipId
  - Slight overhead compared to separate fields
  - Mitigated by caching and the efficiency benefits

## Implementation Notes

### Domain Layer

- **Location**: `lifehacking/Domain/Entities/UserFavorites.cs`
- **No Infrastructure Dependencies**: Entity is persistence-agnostic
- **Immutability**: All properties are read-only
- **Factory Methods**: `Create` for new favorites, `FromPersistence` for rehydration

### Application Layer

- **Interfaces**: `lifehacking/Application/Interfaces/IFavoritesRepository.cs`
- **DTOs**: `lifehacking/Application/Dtos/Favorite/`
- **Use Cases**: `lifehacking/Application/UseCases/Favorite/`
- **Testing**: Unit tests with Moq for mocking, FluentAssertions for assertions

### Infrastructure Layer

- **Firestore Documents**: `lifehacking/Infrastructure/Data/Firestore/FavoriteDocument.cs`
- **Data Store**: `lifehacking/Infrastructure/Data/Firestore/FirestoreFavoriteDataStore.cs`
- **Repository**: `lifehacking/Infrastructure/Repositories/FavoritesRepository.cs`
- **Collection Name**: `FirestoreCollectionNames.Favorites = "favorites"`

### Testing Strategy

- **Unit Tests**: Domain entity behavior, use case logic
- **Property-Based Tests**: Invariant validation (no duplicates, immutability)
- **Integration Tests**: Firestore emulator with per-test collections
- **Test Data**: Use TestDataFactory for realistic test data

### Future Enhancements (Out of Scope)

- **Favorites Limit**: Maximum number of favorites per user
- **Favorites Sharing**: Share favorites between users
- **Favorites Collections**: Organize favorites into folders/collections
- **Favorites Export**: Export favorites to external formats
- **Anonymous Favorites**: Local storage for non-authenticated users
- **Favorites Analytics**: Track popular tips via favorites count

## References

- Issue #10: MVP: add domain model and application contracts for favorites
- ADR-018: Replace PostgreSQL Persistence with Firebase Database
- ADR-019: Firestore Emulator Test Infrastructure and Property-Based Testing
- Existing patterns: User, Tip, Category entities and repositories
