using Application.Dtos.Category;
using Application.Dtos.Dashboard;
using Application.Dtos.Tip;
using Application.Dtos.User;
using Application.Interfaces;
using Application.UseCases.Category;
using Application.UseCases.Tip;
using Application.UseCases.User;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace Application.Tests.BugExploration;

/// <summary>
/// Bug condition exploration tests for cache invalidation issues.
/// These tests are designed to FAIL on unfixed code to demonstrate the bug exists.
/// They verify that dashboard cache and individual category caches are NOT invalidated
/// when entities are modified, causing stale data to be served.
/// 
/// **CRITICAL**: These tests MUST FAIL on unfixed code - failure confirms the bug exists.
/// **DO NOT attempt to fix the tests or the code when they fail**.
/// **NOTE**: These tests encode the expected behavior - they will validate the fix when they pass after implementation.
/// </summary>
public class CacheInvalidationBugExplorationTests
{
    private const string AdminDashboardCacheKey = "AdminDashboard";
    
    /// <summary>
    /// **Validates: Requirements 1.1, 2.1**
    /// Property 1: Fault Condition - Dashboard Cache Not Invalidated on Category Creation
    /// 
    /// This test verifies that when a category is created, the AdminDashboard cache
    /// is NOT invalidated, causing stale category statistics to be displayed.
    /// 
    /// Expected on UNFIXED code: Test FAILS (dashboard cache still contains old data)
    /// Expected on FIXED code: Test PASSES (dashboard cache is invalidated)
    /// </summary>
    [Fact]
    public async Task CreateCategory_ShouldInvalidateDashboardCache_WhenCategoryIsCreated()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var cacheInvalidationService = new CacheInvalidationService(memoryCache);
        
        // Populate dashboard cache with initial data
        var cachedDashboard = new DashboardResponse
        {
            Users = new EntityStatistics { Total = 10 },
            Categories = new EntityStatistics { Total = 5 },
            Tips = new EntityStatistics { Total = 20 }
        };
        memoryCache.Set(AdminDashboardCacheKey, cachedDashboard, TimeSpan.FromDays(1));
        
        // Setup repository mocks
        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync("New Category", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);
        
        categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category c, CancellationToken _) => c);
        
        var useCase = new CreateCategoryUseCase(
            categoryRepositoryMock.Object,
            cacheInvalidationService);
        
        var request = new CreateCategoryRequest("New Category");
        
        // Act
        var result = await useCase.ExecuteAsync(request);
        
        // Assert - Dashboard cache should be invalidated (removed from cache)
        var dashboardCacheExists = memoryCache.TryGetValue(AdminDashboardCacheKey, out DashboardResponse? _);
        
        result.IsSuccess.Should().BeTrue();
        dashboardCacheExists.Should().BeFalse("Dashboard cache should be invalidated after creating category, but it still exists");
    }
    
    /// <summary>
    /// **Validates: Requirements 1.1, 2.1**
    /// Property 1: Fault Condition - Dashboard Cache Not Invalidated on Category Update
    /// 
    /// This test verifies that when a category is updated, the AdminDashboard cache
    /// is NOT invalidated, causing stale category statistics to be displayed.
    /// 
    /// Expected on UNFIXED code: Test FAILS (dashboard cache still contains old data)
    /// Expected on FIXED code: Test PASSES (dashboard cache is invalidated)
    /// </summary>
    [Fact]
    public async Task UpdateCategory_ShouldInvalidateDashboardCache_WhenCategoryIsUpdated()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var tipRepositoryMock = new Mock<ITipRepository>();
        var cacheInvalidationService = new CacheInvalidationService(memoryCache);
        
        // Populate dashboard cache with initial data
        var cachedDashboard = new DashboardResponse
        {
            Users = new EntityStatistics { Total = 10 },
            Categories = new EntityStatistics { Total = 5 },
            Tips = new EntityStatistics { Total = 20 }
        };
        memoryCache.Set(AdminDashboardCacheKey, cachedDashboard, TimeSpan.FromDays(1));
        
        // Setup existing category
        var existingCategory = Category.Create("Old Name");
        var categoryId = existingCategory.Id;
        
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);
        
        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync("New Name", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);
        
        categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        tipRepositoryMock
            .Setup(x => x.CountByCategoryAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        
        var useCase = new UpdateCategoryUseCase(
            categoryRepositoryMock.Object,
            tipRepositoryMock.Object,
            cacheInvalidationService);
        
        var request = new UpdateCategoryRequest("New Name", null);
        
        // Act
        var result = await useCase.ExecuteAsync(categoryId.Value, request);
        
        // Assert - Dashboard cache should be invalidated (removed from cache)
        var dashboardCacheExists = memoryCache.TryGetValue(AdminDashboardCacheKey, out DashboardResponse? _);
        
        result.IsSuccess.Should().BeTrue();
        dashboardCacheExists.Should().BeFalse("Dashboard cache should be invalidated after updating category, but it still exists");
    }
    
    /// <summary>
    /// **Validates: Requirements 1.1, 2.1**
    /// Property 1: Fault Condition - Dashboard Cache Not Invalidated on Category Deletion
    /// 
    /// This test verifies that when a category is deleted, the AdminDashboard cache
    /// is NOT invalidated, causing stale category statistics to be displayed.
    /// 
    /// Expected on UNFIXED code: Test FAILS (dashboard cache still contains old data)
    /// Expected on FIXED code: Test PASSES (dashboard cache is invalidated)
    /// </summary>
    [Fact]
    public async Task DeleteCategory_ShouldInvalidateDashboardCache_WhenCategoryIsDeleted()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var tipRepositoryMock = new Mock<ITipRepository>();
        var cacheInvalidationService = new CacheInvalidationService(memoryCache);
        
        // Populate dashboard cache with initial data
        var cachedDashboard = new DashboardResponse
        {
            Users = new EntityStatistics { Total = 10 },
            Categories = new EntityStatistics { Total = 5 },
            Tips = new EntityStatistics { Total = 20 }
        };
        memoryCache.Set(AdminDashboardCacheKey, cachedDashboard, TimeSpan.FromDays(1));
        
        // Setup existing category
        var existingCategory = Category.Create("Category to Delete");
        var categoryId = existingCategory.Id;
        
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);
        
        categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        tipRepositoryMock
            .Setup(x => x.GetByCategoryAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tip>());
        
        var useCase = new DeleteCategoryUseCase(
            categoryRepositoryMock.Object,
            tipRepositoryMock.Object,
            cacheInvalidationService);
        
        // Act
        var result = await useCase.ExecuteAsync(categoryId.Value);
        
        // Assert - Dashboard cache should be invalidated (removed from cache)
        var dashboardCacheExists = memoryCache.TryGetValue(AdminDashboardCacheKey, out DashboardResponse? _);
        
        result.IsSuccess.Should().BeTrue();
        dashboardCacheExists.Should().BeFalse("Dashboard cache should be invalidated after deleting category, but it still exists");
    }
    
    /// <summary>
    /// **Validates: Requirements 1.2, 1.5, 2.2, 2.5**
    /// Property 1 & 2: Fault Condition - Dashboard and Category Cache Not Invalidated on Tip Creation
    /// 
    /// This test verifies that when a tip is created:
    /// 1. The AdminDashboard cache is NOT invalidated, causing stale tip statistics
    /// 2. The individual Category_{guid} cache is NOT invalidated, causing stale tip counts
    /// 
    /// Expected on UNFIXED code: Test FAILS (both caches still contain old data)
    /// Expected on FIXED code: Test PASSES (both caches are invalidated)
    /// </summary>
    [Fact]
    public async Task CreateTip_ShouldInvalidateDashboardAndCategoryCache_WhenTipIsCreated()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var cacheInvalidationService = new CacheInvalidationService(memoryCache);
        
        // Setup category
        var category = Category.Create("Test Category");
        var categoryId = category.Id;
        var categoryCacheKey = $"Category_{categoryId.Value:D}";
        
        // Populate dashboard cache with initial data
        var cachedDashboard = new DashboardResponse
        {
            Users = new EntityStatistics { Total = 10 },
            Categories = new EntityStatistics { Total = 5 },
            Tips = new EntityStatistics { Total = 20 }
        };
        memoryCache.Set(AdminDashboardCacheKey, cachedDashboard, TimeSpan.FromDays(1));
        
        // Populate individual category cache
        var cachedCategory = new CategoryResponse(categoryId.Value, "Test Category", DateTime.UtcNow, null, null, 5);
        memoryCache.Set(categoryCacheKey, cachedCategory, TimeSpan.FromDays(1));
        
        // Setup repository mocks
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        
        tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tip t, CancellationToken _) => t);
        
        var useCase = new CreateTipUseCase(
            tipRepositoryMock.Object,
            categoryRepositoryMock.Object,
            cacheInvalidationService);
        
        var request = new CreateTipRequest(
            "Test Tip",
            "Test Description",
            new List<TipStepRequest> { new TipStepRequest(1, "This is step 1 with enough characters") },
            categoryId.Value,
            null,
            null,
            null);
        
        // Act
        var result = await useCase.ExecuteAsync(request);
        
        // Assert - Both dashboard and category caches should be invalidated
        var dashboardCacheExists = memoryCache.TryGetValue(AdminDashboardCacheKey, out DashboardResponse? _);
        var categoryCacheExists = memoryCache.TryGetValue(categoryCacheKey, out CategoryResponse? _);
        
        result.IsSuccess.Should().BeTrue();
        dashboardCacheExists.Should().BeFalse("Dashboard cache should be invalidated after creating tip, but it still exists");
        categoryCacheExists.Should().BeFalse("Category cache should be invalidated after creating tip, but it still exists");
    }
    
    /// <summary>
    /// **Validates: Requirements 1.2, 1.5, 2.2, 2.5**
    /// Property 1 & 2: Fault Condition - Dashboard and Category Cache Not Invalidated on Tip Update
    /// 
    /// This test verifies that when a tip is updated:
    /// 1. The AdminDashboard cache is NOT invalidated
    /// 2. The individual Category_{guid} caches are NOT invalidated (both old and new if category changes)
    /// 
    /// Expected on UNFIXED code: Test FAILS (caches still contain old data)
    /// Expected on FIXED code: Test PASSES (caches are invalidated)
    /// </summary>
    [Fact]
    public async Task UpdateTip_ShouldInvalidateDashboardAndCategoryCaches_WhenTipIsUpdated()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var cacheInvalidationService = new CacheInvalidationService(memoryCache);
        
        // Setup categories
        var oldCategory = Category.Create("Old Category");
        var newCategory = Category.Create("New Category");
        var oldCategoryId = oldCategory.Id;
        var newCategoryId = newCategory.Id;
        var oldCategoryCacheKey = $"Category_{oldCategoryId.Value:D}";
        var newCategoryCacheKey = $"Category_{newCategoryId.Value:D}";
        
        // Setup existing tip
        var existingTip = Tip.Create(
            TipTitle.Create("Old Title"),
            TipDescription.Create("Old Description"),
            new List<TipStep> { TipStep.Create(1, "This is step 1 with enough characters") },
            oldCategoryId);
        var tipId = existingTip.Id;
        
        // Populate dashboard cache
        var cachedDashboard = new DashboardResponse
        {
            Users = new EntityStatistics { Total = 10 },
            Categories = new EntityStatistics { Total = 5 },
            Tips = new EntityStatistics { Total = 20 }
        };
        memoryCache.Set(AdminDashboardCacheKey, cachedDashboard, TimeSpan.FromDays(1));
        
        // Populate category caches
        memoryCache.Set(oldCategoryCacheKey, new CategoryResponse(oldCategoryId.Value, "Old Category", DateTime.UtcNow, null, null, 5), TimeSpan.FromDays(1));
        memoryCache.Set(newCategoryCacheKey, new CategoryResponse(newCategoryId.Value, "New Category", DateTime.UtcNow, null, null, 3), TimeSpan.FromDays(1));
        
        // Setup repository mocks
        tipRepositoryMock
            .Setup(x => x.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);
        
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(newCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newCategory);
        
        tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        var useCase = new UpdateTipUseCase(
            tipRepositoryMock.Object,
            categoryRepositoryMock.Object,
            cacheInvalidationService);
        
        var request = new UpdateTipRequest(
            tipId.Value,
            "New Title",
            "New Description",
            new List<TipStepRequest> { new TipStepRequest(1, "This is new step 1 with enough characters") },
            newCategoryId.Value,
            null,
            null,
            null);
        
        // Act
        var result = await useCase.ExecuteAsync(tipId.Value, request);
        
        // Assert - Dashboard and both category caches should be invalidated
        var dashboardCacheExists = memoryCache.TryGetValue(AdminDashboardCacheKey, out DashboardResponse? _);
        var oldCategoryCacheExists = memoryCache.TryGetValue(oldCategoryCacheKey, out CategoryResponse? _);
        var newCategoryCacheExists = memoryCache.TryGetValue(newCategoryCacheKey, out CategoryResponse? _);
        
        result.IsSuccess.Should().BeTrue();
        dashboardCacheExists.Should().BeFalse("Dashboard cache should be invalidated after updating tip, but it still exists");
        oldCategoryCacheExists.Should().BeFalse("Old category cache should be invalidated after tip moved to new category, but it still exists");
        newCategoryCacheExists.Should().BeFalse("New category cache should be invalidated after tip moved from old category, but it still exists");
    }
    
    /// <summary>
    /// **Validates: Requirements 1.2, 1.5, 2.2, 2.5**
    /// Property 1 & 2: Fault Condition - Dashboard and Category Cache Not Invalidated on Tip Deletion
    /// 
    /// This test verifies that when a tip is deleted:
    /// 1. The AdminDashboard cache is NOT invalidated
    /// 2. The individual Category_{guid} cache is NOT invalidated
    /// 
    /// Expected on UNFIXED code: Test FAILS (both caches still contain old data)
    /// Expected on FIXED code: Test PASSES (both caches are invalidated)
    /// </summary>
    [Fact]
    public async Task DeleteTip_ShouldInvalidateDashboardAndCategoryCache_WhenTipIsDeleted()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var tipRepositoryMock = new Mock<ITipRepository>();
        var cacheInvalidationService = new CacheInvalidationService(memoryCache);
        
        // Setup category and tip
        var category = Category.Create("Test Category");
        var categoryId = category.Id;
        var categoryCacheKey = $"Category_{categoryId.Value:D}";
        var existingTip = Tip.Create(
            TipTitle.Create("Tip to delete"),
            TipDescription.Create("Description"),
            new List<TipStep> { TipStep.Create(1, "This is step 1 with enough characters") },
            categoryId);
        var tipId = existingTip.Id;
        
        // Populate dashboard cache
        var cachedDashboard = new DashboardResponse
        {
            Users = new EntityStatistics { Total = 10 },
            Categories = new EntityStatistics { Total = 5 },
            Tips = new EntityStatistics { Total = 20 }
        };
        memoryCache.Set(AdminDashboardCacheKey, cachedDashboard, TimeSpan.FromDays(1));
        
        // Populate category cache
        memoryCache.Set(categoryCacheKey, new CategoryResponse(categoryId.Value, "Test Category", DateTime.UtcNow, null, null, 5), TimeSpan.FromDays(1));
        
        // Setup repository mocks
        tipRepositoryMock
            .Setup(x => x.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);
        
        tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        var useCase = new DeleteTipUseCase(
            tipRepositoryMock.Object,
            cacheInvalidationService);
        
        // Act
        var result = await useCase.ExecuteAsync(tipId.Value);
        
        // Assert - Both dashboard and category caches should be invalidated
        var dashboardCacheExists = memoryCache.TryGetValue(AdminDashboardCacheKey, out DashboardResponse? _);
        var categoryCacheExists = memoryCache.TryGetValue(categoryCacheKey, out CategoryResponse? _);
        
        result.IsSuccess.Should().BeTrue();
        dashboardCacheExists.Should().BeFalse("Dashboard cache should be invalidated after deleting tip, but it still exists");
        categoryCacheExists.Should().BeFalse("Category cache should be invalidated after deleting tip, but it still exists");
    }
    
    /// <summary>
    /// **Validates: Requirements 1.4, 2.4**
    /// Property 1: Fault Condition - Dashboard Cache Not Invalidated on User Deletion
    /// 
    /// This test verifies that when a user is deleted, the AdminDashboard cache
    /// is NOT invalidated, causing stale user statistics to be displayed.
    /// 
    /// Expected on UNFIXED code: Test FAILS (dashboard cache still contains old data)
    /// Expected on FIXED code: Test PASSES (dashboard cache is invalidated)
    /// </summary>
    [Fact]
    public async Task DeleteUser_ShouldInvalidateDashboardCache_WhenUserIsDeleted()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var userRepositoryMock = new Mock<IUserRepository>();
        var userOwnershipServiceMock = new Mock<IUserOwnershipService>();
        var favoritesRepositoryMock = new Mock<IFavoritesRepository>();
        var identityProviderServiceMock = new Mock<IIdentityProviderService>();
        
        // Populate dashboard cache with initial data
        var cachedDashboard = new DashboardResponse
        {
            Users = new EntityStatistics { Total = 20 },
            Categories = new EntityStatistics { Total = 5 },
            Tips = new EntityStatistics { Total = 20 }
        };
        memoryCache.Set(AdminDashboardCacheKey, cachedDashboard, TimeSpan.FromDays(1));
        
        // Setup existing user
        var existingUser = User.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("firebase-uid-123"));
        var userId = existingUser.Id;
        
        userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        
        userOwnershipServiceMock
            .Setup(x => x.EnsureOwnerOrAdminAsync(It.IsAny<User>(), It.IsAny<CurrentUserContext?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Application.Exceptions.AppException?)null);
        
        favoritesRepositoryMock
            .Setup(x => x.RemoveAllByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        
        userRepositoryMock
            .Setup(x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        identityProviderServiceMock
            .Setup(x => x.DeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        var cacheInvalidationService = new CacheInvalidationService(memoryCache);
        
        var useCase = new DeleteUserUseCase(
            userRepositoryMock.Object,
            userOwnershipServiceMock.Object,
            favoritesRepositoryMock.Object,
            identityProviderServiceMock.Object,
            cacheInvalidationService);
        
        var request = new DeleteUserRequest(userId.Value, null);
        
        // Act
        var result = await useCase.ExecuteAsync(request);
        
        // Assert - Dashboard cache should be invalidated (removed from cache)
        var dashboardCacheExists = memoryCache.TryGetValue(AdminDashboardCacheKey, out DashboardResponse? _);
        
        result.IsSuccess.Should().BeTrue();
        dashboardCacheExists.Should().BeFalse("Dashboard cache should be invalidated after deleting user, but it still exists");
    }
}
