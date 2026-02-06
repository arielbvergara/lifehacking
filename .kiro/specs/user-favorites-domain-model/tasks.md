# Implementation Tasks - User Favorites Domain Model

## Phase 1: Domain Layer

### Task 1.1: Create UserFavorites Entity
- [ ] Create `lifehacking/Domain/Entities/UserFavorites.cs`
- [ ] Add private constructor with UserId, TipId, AddedAt
- [ ] Implement `Create(UserId, TipId)` factory method
- [ ] Implement `FromPersistence(...)` factory method for rehydration
- [ ] Add read-only properties: Id (composite), UserId, TipId, AddedAt
- [ ] Ensure no infrastructure dependencies

### Task 1.2: Create Domain Value Objects (if needed)
- [ ] Evaluate if FavoriteId value object is needed (likely not - composite key)
- [ ] Reuse existing UserId and TipId value objects

### Task 1.3: Unit Tests for UserFavorites Entity
- [ ] Test `Create` factory method sets properties correctly
- [ ] Test `Create` sets AddedAt to current UTC time
- [ ] Test `FromPersistence` rehydrates entity correctly
- [ ] Test entity is immutable after creation
- [ ] Verify no infrastructure references in domain tests

## Phase 2: Application Layer - Interfaces

### Task 2.1: Create IFavoritesRepository Interface
- [ ] Create `lifehacking/Application/Interfaces/IFavoritesRepository.cs`
- [ ] Define `GetByUserAndTipAsync(UserId, TipId, CancellationToken)` method
- [ ] Define `AddAsync(UserFavorites, CancellationToken)` method
- [ ] Define `RemoveAsync(UserId, TipId, CancellationToken)` method
- [ ] Define `SearchUserFavoritesAsync(UserId, TipQueryCriteria, CancellationToken)` method
- [ ] Define `ExistsAsync(UserId, TipId, CancellationToken)` method
- [ ] Add XML documentation for all methods

### Task 2.2: Create DTOs for Favorites
- [ ] Create `lifehacking/Application/Dtos/Favorite/AddFavoriteRequest.cs`
- [ ] Create `lifehacking/Application/Dtos/Favorite/RemoveFavoriteRequest.cs`
- [ ] Create `lifehacking/Application/Dtos/Favorite/SearchUserFavoritesRequest.cs`
- [ ] Create `lifehacking/Application/Dtos/Favorite/FavoriteResponse.cs`
- [ ] Create `lifehacking/Application/Dtos/Favorite/PagedFavoritesResponse.cs`
- [ ] Add validation attributes where appropriate

### Task 2.3: Create Extension Methods for Favorites
- [ ] Create `lifehacking/Application/Dtos/Favorite/FavoriteExtensions.cs`
- [ ] Implement `ToFavoriteResponse(UserFavorites, Tip)` extension method
- [ ] Add XML documentation

## Phase 3: Application Layer - Use Cases

### Task 3.1: Create AddFavoriteUseCase
- [ ] Create `lifehacking/Application/UseCases/Favorite/AddFavoriteUseCase.cs`
- [ ] Inject IFavoritesRepository, ITipRepository, IUserRepository
- [ ] Validate user exists
- [ ] Validate tip exists
- [ ] Check if favorite already exists (return ConflictException)
- [ ] Create UserFavorites entity
- [ ] Call repository AddAsync
- [ ] Return Result<FavoriteResponse, AppException>
- [ ] Add XML documentation

### Task 3.2: Create RemoveFavoriteUseCase
- [ ] Create `lifehacking/Application/UseCases/Favorite/RemoveFavoriteUseCase.cs`
- [ ] Inject IFavoritesRepository
- [ ] Validate user exists
- [ ] Check if favorite exists (return NotFoundException if not)
- [ ] Call repository RemoveAsync
- [ ] Return Result<bool, AppException>
- [ ] Add XML documentation

### Task 3.3: Create SearchUserFavoritesUseCase
- [ ] Create `lifehacking/Application/UseCases/Favorite/SearchUserFavoritesUseCase.cs`
- [ ] Inject IFavoritesRepository, ICategoryRepository
- [ ] Validate user exists
- [ ] Call repository SearchUserFavoritesAsync with criteria
- [ ] Fetch category names for tips (batch operation)
- [ ] Map tips to FavoriteResponse DTOs
- [ ] Create pagination metadata
- [ ] Return Result<PagedFavoritesResponse, AppException>
- [ ] Add XML documentation

### Task 3.4: Unit Tests for Use Cases
- [ ] Test AddFavoriteUseCase success scenario
- [ ] Test AddFavoriteUseCase with duplicate favorite (ConflictException)
- [ ] Test AddFavoriteUseCase with non-existent tip (NotFoundException)
- [ ] Test AddFavoriteUseCase with non-existent user (NotFoundException)
- [ ] Test RemoveFavoriteUseCase success scenario
- [ ] Test RemoveFavoriteUseCase with non-existent favorite (NotFoundException)
- [ ] Test SearchUserFavoritesUseCase with various criteria
- [ ] Test SearchUserFavoritesUseCase with empty results
- [ ] Test SearchUserFavoritesUseCase pagination

### Task 3.5: Register Use Cases in DI
- [ ] Update `lifehacking/Application/DependencyInjection.cs`
- [ ] Register AddFavoriteUseCase
- [ ] Register RemoveFavoriteUseCase
- [ ] Register SearchUserFavoritesUseCase

## Phase 4: Infrastructure Layer (Placeholder)

### Task 4.1: Create Firestore Document Model
- [ ] Create `lifehacking/Infrastructure/Data/Firestore/FavoriteDocument.cs`
- [ ] Add properties: UserId, TipId, AddedAt
- [ ] Add Firestore serialization attributes
- [ ] Add conversion methods to/from UserFavorites entity

### Task 4.2: Create Firestore Data Store Interface
- [ ] Create `lifehacking/Infrastructure/Data/Firestore/IFirestoreFavoriteDataStore.cs`
- [ ] Define low-level Firestore operations
- [ ] Mirror IFavoritesRepository methods

### Task 4.3: Implement Firestore Data Store
- [ ] Create `lifehacking/Infrastructure/Data/Firestore/FirestoreFavoriteDataStore.cs`
- [ ] Implement GetByCompositeKeyAsync
- [ ] Implement AddAsync with composite document ID
- [ ] Implement RemoveAsync
- [ ] Implement SearchAsync with query building
- [ ] Implement ExistsAsync
- [ ] Add collection name constant to FirestoreCollectionNames

### Task 4.4: Implement FavoritesRepository
- [ ] Create `lifehacking/Infrastructure/Repositories/FavoritesRepository.cs`
- [ ] Inject IFirestoreFavoriteDataStore, IFirestoreTipDataStore
- [ ] Implement all IFavoritesRepository methods
- [ ] Handle domain/document conversions
- [ ] Add error handling and logging

### Task 4.5: Register Repository in DI
- [ ] Update Infrastructure DI registration
- [ ] Register IFirestoreFavoriteDataStore → FirestoreFavoriteDataStore
- [ ] Register IFavoritesRepository → FavoritesRepository

## Phase 5: Testing

### Task 5.1: Property-Based Tests for Domain
- [ ] Create `lifehacking/Tests/Application.Tests/Domain/Entities/UserFavoritesPropertyTests.cs`
- [ ] Property: Creating favorites with valid inputs always succeeds
- [ ] Property: AddedAt is always in the past or present
- [ ] Property: Rehydrated entities equal original entities

### Task 5.2: Integration Tests for Repository
- [ ] Create `lifehacking/Tests/Infrastructure.Tests/FavoritesRepositoryTests.cs`
- [ ] Test AddAsync creates document with composite key
- [ ] Test RemoveAsync deletes document
- [ ] Test GetByUserAndTipAsync retrieves correct favorite
- [ ] Test ExistsAsync returns correct boolean
- [ ] Test SearchAsync with various criteria
- [ ] Test SearchAsync pagination
- [ ] Test concurrent add operations (idempotency)
- [ ] Use Firestore emulator for all tests

### Task 5.3: Property-Based Tests for Repository
- [ ] Create `lifehacking/Tests/Infrastructure.Tests/FavoritesRepositoryPropertyTests.cs`
- [ ] Property: Adding then removing a favorite leaves no trace
- [ ] Property: Search results always respect sort order
- [ ] Property: Pagination never returns duplicates
- [ ] Property: Total count matches actual number of favorites

## Phase 6: Documentation

### Task 6.1: Create ADR for Favorites Architecture
- [ ] Create `ADRs/020-user-favorites-domain-model-and-storage.md`
- [ ] Document decision to use separate entity vs User property
- [ ] Document Firestore composite key strategy
- [ ] Document no soft-delete rationale
- [ ] Reference issue #10

### Task 6.2: Update AGENTS.md
- [ ] Add UserFavorites entity to domain section
- [ ] Add IFavoritesRepository to application interfaces
- [ ] Add favorites use cases to use cases section

## Phase 7: Build Verification

### Task 7.1: Verify Build
- [ ] Run `dotnet build lifehacking.slnx`
- [ ] Ensure zero errors
- [ ] Ensure zero warnings

### Task 7.2: Run All Tests
- [ ] Run `dotnet test lifehacking.slnx`
- [ ] Ensure all tests pass
- [ ] Verify property-based tests execute

### Task 7.3: Verify No Magic Values
- [ ] Search codebase for hardcoded strings related to favorites
- [ ] Ensure all collection names use constants
- [ ] Ensure all error messages use constants or resources

## Notes
- Each task should result in a single atomic commit
- Follow test naming convention: `{MethodName}_Should{DoSomething}_When{Condition}`
- Use FluentAssertions for all test assertions
- Include XML documentation for all public APIs
- Reference issue #10 in all commit messages: `refs: #10`
