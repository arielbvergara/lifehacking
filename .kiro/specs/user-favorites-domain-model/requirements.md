# User Favorites - Domain Model and Application Contracts

## Overview
Introduce domain and application-level constructs for managing a single favorites list per user. This feature allows users to bookmark tips they want to reference later, with support for add/remove operations and search functionality similar to the existing tips search.

## User Stories

### US-1: Add Tip to Favorites
**As a** registered user  
**I want to** add a tip to my favorites list  
**So that** I can quickly access tips I find useful

**Acceptance Criteria:**
1. User can add any valid tip to their favorites
2. System prevents duplicate favorites (same tip cannot be added twice)
3. System records the timestamp when the tip was added
4. Adding a tip that's already favorited returns a 409 Conflict error
5. Adding a non-existent tip returns a 404 Not Found error
6. Operation is atomic and thread-safe

### US-2: Remove Tip from Favorites
**As a** registered user  
**I want to** remove a tip from my favorites list  
**So that** I can manage my bookmarked content

**Acceptance Criteria:**
1. User can remove any tip from their favorites
2. Removing a tip that's not in favorites returns a 404 Not Found error
3. Removal is permanent (no soft-delete)
4. Operation is atomic and thread-safe
5. Removing a tip does not affect the tip itself

### US-3: View My Favorites
**As a** registered user  
**I want to** view my complete favorites list  
**So that** I can see all tips I've bookmarked

**Acceptance Criteria:**
1. User can retrieve their complete favorites list
2. Favorites are returned in insertion order (most recently added first by default)
3. Each favorite includes the full tip details (title, description, category, etc.)
4. Empty favorites list returns successfully with zero items

### US-4: Search My Favorites
**As a** registered user  
**I want to** search and filter my favorites  
**So that** I can find specific bookmarked tips quickly

**Acceptance Criteria:**
1. User can search favorites by text query (matches title/description)
2. User can filter favorites by category
3. User can filter favorites by tags
4. User can sort favorites by:
   - Date added (default: most recent first)
   - Tip title (alphabetical)
   - Tip creation date
5. Results support pagination (page number, page size)
6. Search returns the same rich tip details as the main tips search

## Domain Requirements

### DR-1: UserFavorites Entity
- Separate domain entity representing a user's favorite tip
- Contains: UserId, TipId, AddedAt timestamp
- No soft-delete (true removal from database)
- Enforces uniqueness constraint: one favorite per (UserId, TipId) pair

### DR-2: Domain Invariants
1. **No Duplicates**: A user cannot favorite the same tip twice
2. **Valid References**: Both UserId and TipId must reference existing entities
3. **Immutable After Creation**: Once added, a favorite's UserId and TipId cannot change
4. **Timestamp Integrity**: AddedAt is set once at creation and never modified

### DR-3: Domain Operations
- `AddFavorite(UserId, TipId)`: Creates a new favorite entry
- `RemoveFavorite(UserId, TipId)`: Permanently deletes a favorite entry
- `GetUserFavorites(UserId, QueryCriteria)`: Retrieves favorites with filtering/sorting
- `IsFavorited(UserId, TipId)`: Checks if a tip is in user's favorites

## Application Layer Requirements

### AR-1: IFavoritesRepository Interface
Define repository contract with methods:
- `Task<UserFavorites?> GetByUserAndTipAsync(UserId userId, TipId tipId, CancellationToken ct)`
- `Task<UserFavorites> AddAsync(UserFavorites favorite, CancellationToken ct)`
- `Task<bool> RemoveAsync(UserId userId, TipId tipId, CancellationToken ct)`
- `Task<(IReadOnlyList<Tip> tips, int totalCount)> SearchUserFavoritesAsync(UserId userId, TipQueryCriteria criteria, CancellationToken ct)`
- `Task<bool> ExistsAsync(UserId userId, TipId tipId, CancellationToken ct)`

### AR-2: Use Cases
1. **AddFavoriteUseCase**: Validates and adds a tip to favorites
2. **RemoveFavoriteUseCase**: Removes a tip from favorites
3. **GetUserFavoritesUseCase**: Retrieves user's favorites with search/filter support

### AR-3: DTOs
- `AddFavoriteRequest`: Contains UserId and TipId
- `RemoveFavoriteRequest`: Contains UserId and TipId
- `SearchUserFavoritesRequest`: Contains UserId and TipQueryCriteria
- `FavoriteResponse`: Contains TipId, AddedAt, and full tip details

### AR-4: Exception Handling
- Throw `ConflictException` when adding duplicate favorite
- Throw `NotFoundException` when:
  - Removing non-existent favorite
  - Referenced tip doesn't exist
  - Referenced user doesn't exist

## Technical Constraints

### TC-1: Firestore Storage
- Store favorites in separate collection: `favorites`
- Document ID format: `{userId}_{tipId}` (composite key)
- Document structure:
  ```json
  {
    "userId": "guid",
    "tipId": "guid",
    "addedAt": "timestamp"
  }
  ```

### TC-2: No Infrastructure Dependencies in Domain
- Domain layer must remain persistence-agnostic
- No Firestore-specific code in domain entities
- Use repository pattern for all data access

### TC-3: Testing Requirements
- Unit tests for domain entity behavior (add/remove/deduplication)
- Unit tests for use case logic
- Property-based tests for invariant validation
- Integration tests with Firestore emulator

## Out of Scope (Future Enhancements)
- Favorites limit per user
- Favorites sharing between users
- Favorites collections/folders
- Favorites export functionality
- Anonymous user favorites (local storage)

## Success Metrics
- Domain code compiles with zero infrastructure references
- All unit tests pass (including property-based tests)
- Integration tests pass with Firestore emulator
- Code follows existing clean architecture patterns
- No magic numbers or strings (use constants)

## References
- Issue: https://github.com/arielbvergara/lifehacking/issues/10
- Milestone: Favorites & Login Merge
- Related: SearchTips functionality (Application/UseCases/Tip/SearchTipsUseCase.cs)
