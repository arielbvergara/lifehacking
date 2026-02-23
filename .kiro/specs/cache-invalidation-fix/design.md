# Cache Invalidation Fix Bugfix Design

## Overview

The cache invalidation system fails to invalidate the AdminDashboard cache when entities (users, categories, tips) are modified, and fails to invalidate individual category caches when tip counts change. This causes stale statistics to be served to users for up to 1 day (the cache TTL). The fix will add dashboard cache invalidation calls to all entity modification use cases and ensure category caches are properly invalidated when tip counts change.

The approach is to:
1. Add the AdminDashboard cache key constant to CacheKeys.cs
2. Add InvalidateDashboard() method to ICacheInvalidationService and its implementation
3. Update all category use cases to invalidate both CategoryList and AdminDashboard caches
4. Update all tip use cases to invalidate CategoryList, individual Category_{guid}, and AdminDashboard caches
5. Update user deletion use case to invalidate AdminDashboard cache

## Glossary

- **Bug_Condition (C)**: The condition that triggers the bug - when entity modifications (create/update/delete) occur but the AdminDashboard cache is not invalidated, or when tip modifications occur but individual category caches are not invalidated
- **Property (P)**: The desired behavior - all relevant caches (AdminDashboard, CategoryList, Category_{guid}) should be invalidated when entities are modified
- **Preservation**: Existing cache invalidation behavior for CategoryList and individual categories must remain unchanged
- **CacheInvalidationService**: The service in `Infrastructure/Services/CacheInvalidationService.cs` that handles cache invalidation using IMemoryCache
- **ICacheInvalidationService**: The interface in `Application/Interfaces/ICacheInvalidationService.cs` that defines cache invalidation operations
- **CacheKeys**: The static class in `Application/Caching/CacheKeys.cs` that defines cache key constants and builders
- **AdminDashboard**: The cached response from GetDashboardUseCase containing user, category, and tip statistics
- **CategoryList**: The cached response from GetCategoriesUseCase containing all categories with tip counts
- **Category_{guid}**: Individual category cache entries for GetCategoryByIdUseCase responses

## Bug Details

### Fault Condition

The bug manifests when entity modification operations (create, update, delete) complete successfully but fail to invalidate the AdminDashboard cache, causing stale statistics to be displayed. Additionally, when tips are modified, individual category caches (Category_{guid}) are not invalidated, causing stale tip counts to persist.

**Formal Specification:**
```
FUNCTION isBugCondition(operation)
  INPUT: operation of type EntityModificationOperation
  OUTPUT: boolean
  
  RETURN (operation.entityType IN ['Category', 'Tip', 'User'])
         AND (operation.type IN ['Create', 'Update', 'Delete'])
         AND (operation.completed == true)
         AND (NOT dashboardCacheInvalidated(operation))
         OR (operation.entityType == 'Tip' 
             AND NOT individualCategoryCacheInvalidated(operation.categoryId))
END FUNCTION
```

### Examples

- **Category Creation**: CreateCategoryUseCase completes successfully and invalidates CategoryList, but AdminDashboard cache still contains old category count (expected: dashboard shows updated count immediately, actual: dashboard shows stale count for up to 1 day)

- **Tip Creation**: CreateTipUseCase completes successfully and invalidates CategoryList and Category_{categoryId}, but AdminDashboard cache still contains old tip count (expected: dashboard shows updated tip count immediately, actual: dashboard shows stale tip count for up to 1 day)

- **Tip Update**: UpdateTipUseCase completes successfully and invalidates CategoryList and affected category caches, but AdminDashboard cache is not invalidated (expected: dashboard reflects any statistical changes, actual: dashboard shows stale data)

- **User Deletion**: DeleteUserUseCase completes successfully but AdminDashboard cache still contains old user count (expected: dashboard shows updated user count immediately, actual: dashboard shows stale user count for up to 1 day)

- **Edge Case - Tip Category Change**: When UpdateTipUseCase moves a tip from one category to another, both the old and new category caches should be invalidated along with CategoryList and AdminDashboard (expected: both category pages show correct tip counts, actual: currently invalidates both categories but not dashboard)

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Existing CategoryList cache invalidation in all category and tip use cases must continue to work
- Existing individual category cache (Category_{guid}) invalidation in category delete and update use cases must continue to work
- The CacheInvalidationService interface and implementation pattern must remain unchanged
- Cache TTL expiration and natural refresh behavior must remain unchanged
- Centralized cache key definitions in CacheKeys class must remain the pattern

**Scope:**
All cache invalidation operations that currently work correctly should be completely unaffected by this fix. This includes:
- CategoryList invalidation in CreateCategoryUseCase, UpdateCategoryUseCase, DeleteCategoryUseCase
- CategoryList and Category_{guid} invalidation in CreateTipUseCase, UpdateTipUseCase, DeleteTipUseCase
- Individual category cache invalidation when categories are deleted or updated
- Cache key generation and formatting in CacheKeys class

## Hypothesized Root Cause

Based on the bug description and code analysis, the root causes are:

1. **Missing Dashboard Cache Key Constant**: The CacheKeys class does not define an AdminDashboard constant, even though GetDashboardUseCase uses a hardcoded "AdminDashboard" string for caching

2. **Missing InvalidateDashboard Method**: The ICacheInvalidationService interface and CacheInvalidationService implementation do not provide a method to invalidate the dashboard cache

3. **Incomplete Cache Invalidation in Use Cases**: Entity modification use cases (category, tip, user) do not call dashboard cache invalidation because the method doesn't exist

4. **Incomplete Category Cache Invalidation**: Tip use cases invalidate CategoryList but the individual Category_{guid} cache invalidation may not be comprehensive across all tip operations

## Correctness Properties

Property 1: Fault Condition - Dashboard Cache Invalidation on Entity Modifications

_For any_ entity modification operation (create, update, delete) on categories, tips, or users that completes successfully, the fixed code SHALL invalidate the AdminDashboard cache, ensuring that subsequent dashboard requests return fresh statistics reflecting the modification.

**Validates: Requirements 2.1, 2.4, 2.5**

Property 2: Fault Condition - Individual Category Cache Invalidation on Tip Modifications

_For any_ tip modification operation (create, update, delete) that completes successfully, the fixed code SHALL invalidate the specific Category_{categoryId} cache entry, ensuring that subsequent GetCategoryById requests return fresh tip counts.

**Validates: Requirements 2.2, 2.3**

Property 3: Preservation - Existing Cache Invalidation Behavior

_For any_ cache invalidation operation that currently works correctly (CategoryList invalidation, existing Category_{guid} invalidation), the fixed code SHALL produce exactly the same cache invalidation behavior as the original code, preserving all existing functionality.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File**: `Application/Caching/CacheKeys.cs`

**Changes**:
1. **Add AdminDashboard Constant**: Add `public const string AdminDashboard = "AdminDashboard";` to match the hardcoded key used in GetDashboardUseCase

**File**: `Application/Interfaces/ICacheInvalidationService.cs`

**Changes**:
1. **Add InvalidateDashboard Method**: Add `void InvalidateDashboard();` method signature to the interface

**File**: `Infrastructure/Services/CacheInvalidationService.cs`

**Changes**:
1. **Implement InvalidateDashboard Method**: Add implementation that calls `_memoryCache.Remove(CacheKeys.AdminDashboard);`

**File**: `Application/UseCases/Category/CreateCategoryUseCase.cs`

**Changes**:
1. **Add Dashboard Invalidation**: After `cacheInvalidationService.InvalidateCategoryList();`, add `cacheInvalidationService.InvalidateDashboard();`

**File**: `Application/UseCases/Category/UpdateCategoryUseCase.cs`

**Changes**:
1. **Add Dashboard Invalidation**: After `cacheInvalidationService.InvalidateCategoryAndList(categoryId);`, add `cacheInvalidationService.InvalidateDashboard();`

**File**: `Application/UseCases/Category/DeleteCategoryUseCase.cs`

**Changes**:
1. **Add Dashboard Invalidation**: After `cacheInvalidationService.InvalidateCategoryAndList(categoryId);`, add `cacheInvalidationService.InvalidateDashboard();`

**File**: `Application/UseCases/Tip/CreateTipUseCase.cs`

**Changes**:
1. **Add Dashboard Invalidation**: After `cacheInvalidationService.InvalidateCategoryAndList(categoryId);`, add `cacheInvalidationService.InvalidateDashboard();`
   - Note: InvalidateCategoryAndList already handles both CategoryList and Category_{guid} invalidation

**File**: `Application/UseCases/Tip/UpdateTipUseCase.cs`

**Changes**:
1. **Add Dashboard Invalidation**: After the existing cache invalidation logic (which handles both old and new categories), add `cacheInvalidationService.InvalidateDashboard();`
   - Note: Existing logic already properly invalidates both categories when category changes

**File**: `Application/UseCases/Tip/DeleteTipUseCase.cs`

**Changes**:
1. **Add Dashboard Invalidation**: After `cacheInvalidationService.InvalidateCategoryAndList(tip.CategoryId);`, add `cacheInvalidationService.InvalidateDashboard();`
   - Note: InvalidateCategoryAndList already handles both CategoryList and Category_{guid} invalidation

**File**: `Application/UseCases/User/DeleteUserUseCase.cs`

**Changes**:
1. **Add ICacheInvalidationService Dependency**: Add `ICacheInvalidationService cacheInvalidationService` to constructor parameters
2. **Add Dashboard Invalidation**: After successful user deletion (after repository update), add `cacheInvalidationService.InvalidateDashboard();`

## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, write tests that demonstrate the bug on unfixed code (exploratory fault condition checking), then verify the fix works correctly and preserves existing behavior.

### Exploratory Fault Condition Checking

**Goal**: Surface counterexamples that demonstrate the bug BEFORE implementing the fix. Confirm that dashboard cache is not invalidated when entities are modified, and that individual category caches are not invalidated when tips are modified.

**Test Plan**: Write integration tests that:
1. Populate the dashboard cache by calling GetDashboardUseCase
2. Perform entity modification operations (create/update/delete)
3. Verify that the dashboard cache still contains stale data (test will pass on unfixed code, demonstrating the bug)
4. Verify that individual category caches still contain stale tip counts (test will pass on unfixed code)

Run these tests on the UNFIXED code to observe that caches are NOT invalidated, confirming the bug.

**Test Cases**:
1. **Category Creation Dashboard Bug**: Create category, verify dashboard cache still shows old category count (will demonstrate bug on unfixed code)
2. **Tip Creation Dashboard Bug**: Create tip, verify dashboard cache still shows old tip count (will demonstrate bug on unfixed code)
3. **Tip Creation Category Cache Bug**: Create tip, verify Category_{guid} cache still shows old tip count (will demonstrate bug on unfixed code)
4. **User Deletion Dashboard Bug**: Delete user, verify dashboard cache still shows old user count (will demonstrate bug on unfixed code)
5. **Tip Update Dashboard Bug**: Update tip, verify dashboard cache not invalidated (will demonstrate bug on unfixed code)

**Expected Counterexamples**:
- Dashboard cache contains stale statistics after entity modifications
- Individual category caches contain stale tip counts after tip modifications
- Possible causes: missing InvalidateDashboard() method, missing cache key constant, missing invalidation calls in use cases

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds (entity modifications), the fixed code properly invalidates all relevant caches.

**Pseudocode:**
```
FOR ALL operation WHERE isBugCondition(operation) DO
  result := executeOperation_fixed(operation)
  ASSERT dashboardCacheInvalidated(operation)
  IF operation.entityType == 'Tip' THEN
    ASSERT individualCategoryCacheInvalidated(operation.categoryId)
  END IF
END FOR
```

**Test Plan**: Write integration tests that:
1. Populate caches (dashboard, category list, individual categories)
2. Perform entity modification operations
3. Verify that all relevant caches are invalidated (cache entries should be removed)
4. Verify that subsequent requests return fresh data

**Test Cases**:
1. **Category Creation Invalidates Dashboard**: Create category, verify dashboard cache is removed
2. **Category Update Invalidates Dashboard**: Update category, verify dashboard cache is removed
3. **Category Delete Invalidates Dashboard**: Delete category, verify dashboard cache is removed
4. **Tip Creation Invalidates Dashboard and Category**: Create tip, verify both dashboard and Category_{guid} caches are removed
5. **Tip Update Invalidates Dashboard and Categories**: Update tip (including category change), verify dashboard and both old/new category caches are removed
6. **Tip Delete Invalidates Dashboard and Category**: Delete tip, verify both dashboard and Category_{guid} caches are removed
7. **User Delete Invalidates Dashboard**: Delete user, verify dashboard cache is removed

### Preservation Checking

**Goal**: Verify that for all cache invalidation operations that currently work correctly, the fixed code produces the same result as the original code.

**Pseudocode:**
```
FOR ALL operation WHERE existingCacheInvalidationWorks(operation) DO
  ASSERT cacheInvalidation_original(operation) = cacheInvalidation_fixed(operation)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation checking because:
- It generates many test cases automatically across the input domain
- It catches edge cases that manual unit tests might miss
- It provides strong guarantees that behavior is unchanged for existing cache invalidation

**Test Plan**: Write tests that verify existing cache invalidation behavior continues to work:
1. Observe that CategoryList is invalidated on unfixed code for category/tip operations
2. Observe that Category_{guid} is invalidated on unfixed code for category delete/update
3. Write tests to verify this behavior continues after fix

**Test Cases**:
1. **CategoryList Invalidation Preserved**: Verify category and tip operations still invalidate CategoryList cache
2. **Individual Category Invalidation Preserved**: Verify category delete/update still invalidates Category_{guid} cache
3. **Cache Key Generation Preserved**: Verify CacheKeys.Category() still generates correct keys
4. **Service Interface Preserved**: Verify existing ICacheInvalidationService methods still work

### Unit Tests

- Test InvalidateDashboard() method in CacheInvalidationService
- Test that AdminDashboard constant is correctly defined in CacheKeys
- Test each use case's cache invalidation calls (mock ICacheInvalidationService)
- Test edge cases (null category IDs, category changes in tip updates)

### Property-Based Tests

- Generate random entity modification sequences and verify all caches are properly invalidated
- Generate random cache states and verify preservation of existing invalidation behavior
- Test across many scenarios to ensure no edge cases are missed

### Integration Tests

- Test full entity modification flows with actual cache population and invalidation
- Test dashboard refresh after entity modifications shows correct statistics
- Test category page refresh after tip modifications shows correct tip counts
- Test that cache TTL expiration still works as expected
