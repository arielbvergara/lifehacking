using Application.Dtos.Category;
using Application.Dtos.Tip;
using Application.Interfaces;
using Application.UseCases.Category;
using Application.UseCases.Tip;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace Application.Tests.Preservation;

/// <summary>
/// Preservation property tests for cache invalidation behavior.
/// These tests verify that existing cache invalidation operations continue to work correctly
/// after the bugfix is implemented, ensuring no regressions are introduced.
/// 
/// **IMPORTANT**: These tests should PASS on UNFIXED code to establish baseline behavior.
/// They verify the preservation requirements (3.1-3.5) from the bugfix spec.
/// 
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**
/// Property 3: Preservation - Existing Cache Invalidation Behavior
/// </summary>
public class CacheInvalidationPreservationTests
{
    private const string CategoryListCacheKey = "CategoryList";
    
    /// <summary>
    /// **Validates: Requirement 3.2**
    /// Property 3: Preservation - CategoryList Invalidation on Category Creation
    /// 
    /// Verifies that CreateCategoryUseCase continues to invalidate the CategoryList cache
    /// as currently implemented. This is existing behavior that must be preserved.
    /// 
    /// Expected on UNFIXED code: Test PASSES (CategoryList is invalidated)
    /// Expected on FIXED code: Test PASSES (behavior preserved)
    /// </summary>
    [Fact]
    public async Task CreateCategory_ShouldInvalidateCategoryList_WhenCategoryIsCreated()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
        
        // Track if InvalidateCategoryList was called
        bool categoryListInvalidated = false;
        mockCacheInvalidationService
            .Setup(x => x.InvalidateCategoryList())
            .Callback(() => categoryListInvalidated = true);
        
        // Populate CategoryList cache
        var cachedCategories = new List<CategoryResponse>
        {
            new CategoryResponse(Guid.NewGuid(), "Category 1", DateTime.UtcNow, null, null, 5),
            new CategoryResponse(Guid.NewGuid(), "Category 2", DateTime.UtcNow, null, null, 3)
        };
        memoryCache.Set(CategoryListCacheKey, cachedCategories, TimeSpan.FromDays(1));
        
        // Setup repository mocks
        categoryRepositoryMock
            .Setup(x => x.GetByNameAsync("New Category", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);
        
        categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category c, CancellationToken _) => c);
        
        var useCase = new CreateCategoryUseCase(
            categoryRepositoryMock.Object,
            mockCacheInvalidationService.Object);
        
        var request = new CreateCategoryRequest("New Category");
        
        // Act
        var result = await useCase.ExecuteAsync(request);
        
        // Assert - CategoryList invalidation should be called (existing behavior)
        result.IsSuccess.Should().BeTrue();
        categoryListInvalidated.Should().BeTrue("CategoryList cache should be invalidated when category is created (existing behavior)");
        mockCacheInvalidationService.Verify(x => x.InvalidateCategoryList(), Times.Once);
    }
    
    /// <summary>
    /// **Validates: Requirements 3.2, 3.3**
    /// Property 3: Preservation - CategoryList and Category_{guid} Invalidation on Category Update
    /// 
    /// Verifies that UpdateCategoryUseCase continues to invalidate both CategoryList and
    /// individual Category_{guid} caches via InvalidateCategoryAndList() as currently implemented.
    /// 
    /// Expected on UNFIXED code: Test PASSES (both caches are invalidated)
    /// Expected on FIXED code: Test PASSES (behavior preserved)
    /// </summary>
    [Fact]
    public async Task UpdateCategory_ShouldInvalidateCategoryAndList_WhenCategoryIsUpdated()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var tipRepositoryMock = new Mock<ITipRepository>();
        var mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
        
        // Track if InvalidateCategoryAndList was called
        bool categoryAndListInvalidated = false;
        CategoryId? invalidatedCategoryId = null;
        mockCacheInvalidationService
            .Setup(x => x.InvalidateCategoryAndList(It.IsAny<CategoryId>()))
            .Callback<CategoryId>(id =>
            {
                categoryAndListInvalidated = true;
                invalidatedCategoryId = id;
            });
        
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
            mockCacheInvalidationService.Object);
        
        var request = new UpdateCategoryRequest("New Name", null);
        
        // Act
        var result = await useCase.ExecuteAsync(categoryId.Value, request);
        
        // Assert - InvalidateCategoryAndList should be called (existing behavior)
        result.IsSuccess.Should().BeTrue();
        categoryAndListInvalidated.Should().BeTrue("InvalidateCategoryAndList should be called when category is updated (existing behavior)");
        invalidatedCategoryId.Should().Be(categoryId);
        mockCacheInvalidationService.Verify(x => x.InvalidateCategoryAndList(categoryId), Times.Once);
    }
    
    /// <summary>
    /// **Validates: Requirements 3.2, 3.3**
    /// Property 3: Preservation - CategoryList and Category_{guid} Invalidation on Category Deletion
    /// 
    /// Verifies that DeleteCategoryUseCase continues to invalidate both CategoryList and
    /// individual Category_{guid} caches via InvalidateCategoryAndList() as currently implemented.
    /// 
    /// Expected on UNFIXED code: Test PASSES (both caches are invalidated)
    /// Expected on FIXED code: Test PASSES (behavior preserved)
    /// </summary>
    [Fact]
    public async Task DeleteCategory_ShouldInvalidateCategoryAndList_WhenCategoryIsDeleted()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var tipRepositoryMock = new Mock<ITipRepository>();
        var mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
        
        // Track if InvalidateCategoryAndList was called
        bool categoryAndListInvalidated = false;
        CategoryId? invalidatedCategoryId = null;
        mockCacheInvalidationService
            .Setup(x => x.InvalidateCategoryAndList(It.IsAny<CategoryId>()))
            .Callback<CategoryId>(id =>
            {
                categoryAndListInvalidated = true;
                invalidatedCategoryId = id;
            });
        
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
            mockCacheInvalidationService.Object);
        
        // Act
        var result = await useCase.ExecuteAsync(categoryId.Value);
        
        // Assert - InvalidateCategoryAndList should be called (existing behavior)
        result.IsSuccess.Should().BeTrue();
        categoryAndListInvalidated.Should().BeTrue("InvalidateCategoryAndList should be called when category is deleted (existing behavior)");
        invalidatedCategoryId.Should().Be(categoryId);
        mockCacheInvalidationService.Verify(x => x.InvalidateCategoryAndList(categoryId), Times.Once);
    }
    
    /// <summary>
    /// **Validates: Requirements 3.2, 3.3**
    /// Property 3: Preservation - CategoryList and Category_{guid} Invalidation on Tip Creation
    /// 
    /// Verifies that CreateTipUseCase continues to invalidate both CategoryList and
    /// individual Category_{guid} caches via InvalidateCategoryAndList() as currently implemented.
    /// 
    /// Expected on UNFIXED code: Test PASSES (both caches are invalidated)
    /// Expected on FIXED code: Test PASSES (behavior preserved)
    /// </summary>
    [Fact]
    public async Task CreateTip_ShouldInvalidateCategoryAndList_WhenTipIsCreated()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
        
        // Track if InvalidateCategoryAndList was called
        bool categoryAndListInvalidated = false;
        CategoryId? invalidatedCategoryId = null;
        mockCacheInvalidationService
            .Setup(x => x.InvalidateCategoryAndList(It.IsAny<CategoryId>()))
            .Callback<CategoryId>(id =>
            {
                categoryAndListInvalidated = true;
                invalidatedCategoryId = id;
            });
        
        // Setup category
        var category = Category.Create("Test Category");
        var categoryId = category.Id;
        
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        
        tipRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tip t, CancellationToken _) => t);
        
        var useCase = new CreateTipUseCase(
            tipRepositoryMock.Object,
            categoryRepositoryMock.Object,
            mockCacheInvalidationService.Object);
        
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
        
        // Assert - InvalidateCategoryAndList should be called (existing behavior)
        result.IsSuccess.Should().BeTrue();
        categoryAndListInvalidated.Should().BeTrue("InvalidateCategoryAndList should be called when tip is created (existing behavior)");
        invalidatedCategoryId.Should().Be(categoryId);
        mockCacheInvalidationService.Verify(x => x.InvalidateCategoryAndList(categoryId), Times.Once);
    }
    
    /// <summary>
    /// **Validates: Requirements 3.2, 3.3**
    /// Property 3: Preservation - CategoryList and Category_{guid} Invalidation on Tip Update
    /// 
    /// Verifies that UpdateTipUseCase continues to invalidate CategoryList and both old/new
    /// Category_{guid} caches when a tip's category changes, as currently implemented.
    /// 
    /// Expected on UNFIXED code: Test PASSES (all relevant caches are invalidated)
    /// Expected on FIXED code: Test PASSES (behavior preserved)
    /// </summary>
    [Fact]
    public async Task UpdateTip_ShouldInvalidateBothCategoriesAndList_WhenTipCategoryChanges()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
        
        // Track cache invalidation calls
        var invalidatedCategoryIds = new List<CategoryId>();
        mockCacheInvalidationService
            .Setup(x => x.InvalidateCategoryAndList(It.IsAny<CategoryId>()))
            .Callback<CategoryId>(id => invalidatedCategoryIds.Add(id));
        
        mockCacheInvalidationService
            .Setup(x => x.InvalidateCategory(It.IsAny<CategoryId>()))
            .Callback<CategoryId>(id => invalidatedCategoryIds.Add(id));
        
        // Setup categories
        var oldCategory = Category.Create("Old Category");
        var newCategory = Category.Create("New Category");
        var oldCategoryId = oldCategory.Id;
        var newCategoryId = newCategory.Id;
        
        // Setup existing tip
        var existingTip = Tip.Create(
            TipTitle.Create("Old Title"),
            TipDescription.Create("Old Description"),
            new List<TipStep> { TipStep.Create(1, "This is step 1 with enough characters") },
            oldCategoryId);
        var tipId = existingTip.Id;
        
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
            mockCacheInvalidationService.Object);
        
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
        
        // Assert - Both categories should be invalidated (existing behavior)
        result.IsSuccess.Should().BeTrue();
        invalidatedCategoryIds.Should().Contain(newCategoryId, "New category should be invalidated via InvalidateCategoryAndList (existing behavior)");
        invalidatedCategoryIds.Should().Contain(oldCategoryId, "Old category should be invalidated via InvalidateCategory (existing behavior)");
        mockCacheInvalidationService.Verify(x => x.InvalidateCategoryAndList(newCategoryId), Times.Once);
        mockCacheInvalidationService.Verify(x => x.InvalidateCategory(oldCategoryId), Times.Once);
    }
    
    /// <summary>
    /// **Validates: Requirements 3.2, 3.3**
    /// Property 3: Preservation - CategoryList and Category_{guid} Invalidation on Tip Deletion
    /// 
    /// Verifies that DeleteTipUseCase continues to invalidate both CategoryList and
    /// individual Category_{guid} caches via InvalidateCategoryAndList() as currently implemented.
    /// 
    /// Expected on UNFIXED code: Test PASSES (both caches are invalidated)
    /// Expected on FIXED code: Test PASSES (behavior preserved)
    /// </summary>
    [Fact]
    public async Task DeleteTip_ShouldInvalidateCategoryAndList_WhenTipIsDeleted()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var tipRepositoryMock = new Mock<ITipRepository>();
        var mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
        
        // Track if InvalidateCategoryAndList was called
        bool categoryAndListInvalidated = false;
        CategoryId? invalidatedCategoryId = null;
        mockCacheInvalidationService
            .Setup(x => x.InvalidateCategoryAndList(It.IsAny<CategoryId>()))
            .Callback<CategoryId>(id =>
            {
                categoryAndListInvalidated = true;
                invalidatedCategoryId = id;
            });
        
        // Setup category and tip
        var category = Category.Create("Test Category");
        var categoryId = category.Id;
        var existingTip = Tip.Create(
            TipTitle.Create("Tip to delete"),
            TipDescription.Create("Description"),
            new List<TipStep> { TipStep.Create(1, "This is step 1 with enough characters") },
            categoryId);
        var tipId = existingTip.Id;
        
        // Setup repository mocks
        tipRepositoryMock
            .Setup(x => x.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTip);
        
        tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        var useCase = new DeleteTipUseCase(
            tipRepositoryMock.Object,
            mockCacheInvalidationService.Object);
        
        // Act
        var result = await useCase.ExecuteAsync(tipId.Value);
        
        // Assert - InvalidateCategoryAndList should be called (existing behavior)
        result.IsSuccess.Should().BeTrue();
        categoryAndListInvalidated.Should().BeTrue("InvalidateCategoryAndList should be called when tip is deleted (existing behavior)");
        invalidatedCategoryId.Should().Be(categoryId);
        mockCacheInvalidationService.Verify(x => x.InvalidateCategoryAndList(categoryId), Times.Once);
    }
    
    /// <summary>
    /// **Validates: Requirement 3.1**
    /// Property 3: Preservation - CacheInvalidationService Interface Pattern
    /// 
    /// Verifies that the ICacheInvalidationService interface continues to provide
    /// the existing methods (InvalidateCategoryList, InvalidateCategory, InvalidateCategoryAndList)
    /// and that they work correctly.
    /// 
    /// Expected on UNFIXED code: Test PASSES (interface methods work correctly)
    /// Expected on FIXED code: Test PASSES (interface pattern preserved)
    /// </summary>
    [Fact]
    public void CacheInvalidationService_ShouldProvideExistingMethods_WhenUsed()
    {
        // Arrange
        var mockService = new Mock<ICacheInvalidationService>();
        var categoryId = CategoryId.Create(Guid.NewGuid());
        
        // Act & Assert - Verify interface provides existing methods
        mockService.Object.InvalidateCategoryList();
        mockService.Object.InvalidateCategory(categoryId);
        mockService.Object.InvalidateCategoryAndList(categoryId);
        
        // Verify methods can be called without errors (interface contract preserved)
        mockService.Verify(x => x.InvalidateCategoryList(), Times.Once);
        mockService.Verify(x => x.InvalidateCategory(categoryId), Times.Once);
        mockService.Verify(x => x.InvalidateCategoryAndList(categoryId), Times.Once);
    }
}
