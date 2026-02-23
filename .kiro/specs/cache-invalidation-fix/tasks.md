# Implementation Plan

- [x] 1. Write bug condition exploration tests
  - **Property 1: Fault Condition** - Dashboard and Category Cache Not Invalidated on Entity Modifications
  - **CRITICAL**: These tests MUST FAIL on unfixed code - failure confirms the bug exists
  - **DO NOT attempt to fix the tests or the code when they fail**
  - **NOTE**: These tests encode the expected behavior - they will validate the fix when they pass after implementation
  - **GOAL**: Surface counterexamples that demonstrate the bug exists
  - **Scoped PBT Approach**: Scope properties to concrete failing cases (specific entity modifications)
  - Test that dashboard cache is NOT invalidated when categories are created/updated/deleted (from Fault Condition in design)
  - Test that dashboard cache is NOT invalidated when tips are created/updated/deleted (from Fault Condition in design)
  - Test that dashboard cache is NOT invalidated when users are deleted (from Fault Condition in design)
  - Test that individual category caches (Category_{guid}) are NOT invalidated when tips are created/updated/deleted (from Fault Condition in design)
  - The test assertions should match the Expected Behavior Properties from design (Properties 1 and 2)
  - Run tests on UNFIXED code
  - **EXPECTED OUTCOME**: Tests FAIL (this is correct - it proves the bug exists)
  - Document counterexamples found (e.g., "CreateCategoryUseCase completes but dashboard cache still contains old category count")
  - Mark task complete when tests are written, run, and failures are documented
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 2. Write preservation property tests (BEFORE implementing fix)
  - **Property 2: Preservation** - Existing Cache Invalidation Behavior
  - **IMPORTANT**: Follow observation-first methodology
  - Observe behavior on UNFIXED code for existing cache invalidation operations
  - Observe: CategoryList is invalidated when categories are created/updated/deleted on unfixed code
  - Observe: CategoryList is invalidated when tips are created/updated/deleted on unfixed code
  - Observe: Category_{guid} is invalidated when categories are deleted/updated on unfixed code
  - Observe: InvalidateCategoryAndList() properly invalidates both CategoryList and Category_{guid} on unfixed code
  - Write property-based tests capturing observed behavior patterns from Preservation Requirements
  - Property-based testing generates many test cases for stronger guarantees
  - Run tests on UNFIXED code
  - **EXPECTED OUTCOME**: Tests PASS (this confirms baseline behavior to preserve)
  - Mark task complete when tests are written, run, and passing on unfixed code
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 3. Fix for cache invalidation bug

  - [x] 3.1 Add AdminDashboard cache key constant to CacheKeys.cs
    - Add `public const string AdminDashboard = "AdminDashboard";` to Application/Caching/CacheKeys.cs
    - This matches the hardcoded key used in GetDashboardUseCase
    - _Bug_Condition: isBugCondition(operation) where operation modifies categories, tips, or users_
    - _Expected_Behavior: AdminDashboard cache SHALL be invalidated for all entity modifications (Property 1)_
    - _Preservation: Existing CacheKeys class pattern SHALL remain unchanged (Requirement 3.5)_
    - _Requirements: 2.1, 2.4, 2.5, 3.5_

  - [x] 3.2 Add InvalidateDashboard method to ICacheInvalidationService interface
    - Add `void InvalidateDashboard();` method signature to Application/Interfaces/ICacheInvalidationService.cs
    - _Bug_Condition: isBugCondition(operation) where operation modifies categories, tips, or users_
    - _Expected_Behavior: Interface SHALL provide method to invalidate dashboard cache (Property 1)_
    - _Preservation: Existing ICacheInvalidationService interface pattern SHALL remain unchanged (Requirement 3.1)_
    - _Requirements: 2.1, 2.4, 2.5, 3.1_

  - [x] 3.3 Implement InvalidateDashboard method in CacheInvalidationService
    - Add implementation in Infrastructure/Services/CacheInvalidationService.cs
    - Implementation: `_memoryCache.Remove(CacheKeys.AdminDashboard);`
    - _Bug_Condition: isBugCondition(operation) where operation modifies categories, tips, or users_
    - _Expected_Behavior: Implementation SHALL remove AdminDashboard cache entry (Property 1)_
    - _Preservation: Existing CacheInvalidationService implementation pattern SHALL remain unchanged (Requirement 3.1)_
    - _Requirements: 2.1, 2.4, 2.5, 3.1_

  - [x] 3.4 Update CreateCategoryUseCase to invalidate dashboard cache
    - Add `cacheInvalidationService.InvalidateDashboard();` after `cacheInvalidationService.InvalidateCategoryList();`
    - File: Application/UseCases/Category/CreateCategoryUseCase.cs
    - _Bug_Condition: isBugCondition(operation) where operation.entityType == 'Category' AND operation.type == 'Create'_
    - _Expected_Behavior: Dashboard cache SHALL be invalidated when categories are created (Property 1, Requirement 2.1)_
    - _Preservation: Existing CategoryList invalidation SHALL remain unchanged (Requirement 3.2)_
    - _Requirements: 2.1, 2.6, 3.2_

  - [x] 3.5 Update UpdateCategoryUseCase to invalidate dashboard cache
    - Add `cacheInvalidationService.InvalidateDashboard();` after `cacheInvalidationService.InvalidateCategoryAndList(categoryId);`
    - File: Application/UseCases/Category/UpdateCategoryUseCase.cs
    - _Bug_Condition: isBugCondition(operation) where operation.entityType == 'Category' AND operation.type == 'Update'_
    - _Expected_Behavior: Dashboard cache SHALL be invalidated when categories are updated (Property 1, Requirement 2.1)_
    - _Preservation: Existing CategoryList and Category_{guid} invalidation SHALL remain unchanged (Requirements 3.2, 3.3)_
    - _Requirements: 2.1, 2.6, 3.2, 3.3_

  - [x] 3.6 Update DeleteCategoryUseCase to invalidate dashboard cache
    - Add `cacheInvalidationService.InvalidateDashboard();` after `cacheInvalidationService.InvalidateCategoryAndList(categoryId);`
    - File: Application/UseCases/Category/DeleteCategoryUseCase.cs
    - _Bug_Condition: isBugCondition(operation) where operation.entityType == 'Category' AND operation.type == 'Delete'_
    - _Expected_Behavior: Dashboard cache SHALL be invalidated when categories are deleted (Property 1, Requirement 2.1)_
    - _Preservation: Existing CategoryList and Category_{guid} invalidation SHALL remain unchanged (Requirements 3.2, 3.3)_
    - _Requirements: 2.1, 2.6, 3.2, 3.3_

  - [x] 3.7 Update CreateTipUseCase to invalidate dashboard cache
    - Add `cacheInvalidationService.InvalidateDashboard();` after `cacheInvalidationService.InvalidateCategoryAndList(categoryId);`
    - File: Application/UseCases/Tip/CreateTipUseCase.cs
    - Note: InvalidateCategoryAndList already handles both CategoryList and Category_{guid} invalidation
    - _Bug_Condition: isBugCondition(operation) where operation.entityType == 'Tip' AND operation.type == 'Create'_
    - _Expected_Behavior: Dashboard cache SHALL be invalidated when tips are created (Property 1, Requirement 2.5); Category_{guid} cache SHALL be invalidated (Property 2, Requirement 2.2)_
    - _Preservation: Existing CategoryList and Category_{guid} invalidation SHALL remain unchanged (Requirements 3.2, 3.3)_
    - _Requirements: 2.2, 2.3, 2.5, 3.2, 3.3_

  - [x] 3.8 Update UpdateTipUseCase to invalidate dashboard cache
    - Add `cacheInvalidationService.InvalidateDashboard();` after existing cache invalidation logic
    - File: Application/UseCases/Tip/UpdateTipUseCase.cs
    - Note: Existing logic already properly invalidates both old and new categories when category changes
    - _Bug_Condition: isBugCondition(operation) where operation.entityType == 'Tip' AND operation.type == 'Update'_
    - _Expected_Behavior: Dashboard cache SHALL be invalidated when tips are updated (Property 1, Requirement 2.5); Category_{guid} cache SHALL be invalidated (Property 2, Requirement 2.2)_
    - _Preservation: Existing CategoryList and Category_{guid} invalidation SHALL remain unchanged (Requirements 3.2, 3.3)_
    - _Requirements: 2.2, 2.3, 2.5, 3.2, 3.3_

  - [x] 3.9 Update DeleteTipUseCase to invalidate dashboard cache
    - Add `cacheInvalidationService.InvalidateDashboard();` after `cacheInvalidationService.InvalidateCategoryAndList(tip.CategoryId);`
    - File: Application/UseCases/Tip/DeleteTipUseCase.cs
    - Note: InvalidateCategoryAndList already handles both CategoryList and Category_{guid} invalidation
    - _Bug_Condition: isBugCondition(operation) where operation.entityType == 'Tip' AND operation.type == 'Delete'_
    - _Expected_Behavior: Dashboard cache SHALL be invalidated when tips are deleted (Property 1, Requirement 2.5); Category_{guid} cache SHALL be invalidated (Property 2, Requirement 2.2)_
    - _Preservation: Existing CategoryList and Category_{guid} invalidation SHALL remain unchanged (Requirements 3.2, 3.3)_
    - _Requirements: 2.2, 2.3, 2.5, 3.2, 3.3_

  - [x] 3.10 Update DeleteUserUseCase to add cache invalidation
    - Add `ICacheInvalidationService cacheInvalidationService` to constructor parameters
    - Add `cacheInvalidationService.InvalidateDashboard();` after successful user deletion
    - File: Application/UseCases/User/DeleteUserUseCase.cs
    - _Bug_Condition: isBugCondition(operation) where operation.entityType == 'User' AND operation.type == 'Delete'_
    - _Expected_Behavior: Dashboard cache SHALL be invalidated when users are deleted (Property 1, Requirement 2.4)_
    - _Preservation: Existing use case pattern SHALL remain unchanged (Requirement 3.1)_
    - _Requirements: 2.4, 3.1_

  - [x] 3.11 Verify bug condition exploration tests now pass
    - **Property 1: Expected Behavior** - Dashboard and Category Cache Invalidated on Entity Modifications
    - **IMPORTANT**: Re-run the SAME tests from task 1 - do NOT write new tests
    - The tests from task 1 encode the expected behavior
    - When these tests pass, it confirms the expected behavior is satisfied
    - Run bug condition exploration tests from step 1
    - **EXPECTED OUTCOME**: Tests PASS (confirms bug is fixed)
    - Verify dashboard cache is invalidated when categories are created/updated/deleted
    - Verify dashboard cache is invalidated when tips are created/updated/deleted
    - Verify dashboard cache is invalidated when users are deleted
    - Verify individual category caches (Category_{guid}) are invalidated when tips are created/updated/deleted
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [x] 3.12 Verify preservation tests still pass
    - **Property 2: Preservation** - Existing Cache Invalidation Behavior
    - **IMPORTANT**: Re-run the SAME tests from task 2 - do NOT write new tests
    - Run preservation property tests from step 2
    - **EXPECTED OUTCOME**: Tests PASS (confirms no regressions)
    - Confirm CategoryList invalidation still works for category operations
    - Confirm CategoryList invalidation still works for tip operations
    - Confirm Category_{guid} invalidation still works for category delete/update
    - Confirm InvalidateCategoryAndList() still works correctly
    - Confirm all tests still pass after fix (no regressions)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 4. Checkpoint - Ensure all tests pass
  - Run all tests to ensure no regressions
  - Verify dashboard cache is properly invalidated for all entity modifications
  - Verify individual category caches are properly invalidated for tip modifications
  - Verify existing cache invalidation behavior is preserved
  - Ask the user if questions arise
