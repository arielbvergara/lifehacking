using Application.Interfaces;
using Application.UseCases.Category;
using Domain.ValueObject;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using Xunit;
using DomainCategory = Domain.Entities.Category;
using DomainTip = Domain.Entities.Tip;

namespace Application.Tests.UseCases.Category;

/// <summary>
/// Property-based tests for DeleteCategoryUseCase.
/// Feature: admin-category-management
/// 
/// These tests verify universal properties that should hold across all valid inputs
/// using FsCheck to generate random test data and run 100+ iterations per property.
/// </summary>
public class DeleteCategoryUseCasePropertyTests
{
    // Feature: admin-category-management, Property 8: Cascade soft-delete to associated tips
    // For any category with associated tips, soft-deleting the category should result in all 
    // associated tips also being soft-deleted (IsDeleted=true, DeletedAt set).
    // Validates: Requirements 3.5, 3.7

    /// <summary>
    /// Property: Deleting a category should cascade soft-delete to all associated tips.
    /// Both the category and all tips should have IsDeleted=true and DeletedAt set.
    /// **Validates: Requirements 3.5, 3.7**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-category-management")]
    [Trait("Property", "Property 8: Cascade soft-delete to associated tips")]
    public async Task CascadeSoftDelete_ShouldMarkCategoryAndAllTipsAsDeleted_WhenDeletingCategory(
        NonEmptyString categoryNameGen,
        PositiveInt tipCountGen)
    {
        // Precondition: Ensure category name meets minimum length requirements (2-100 characters after trimming)
        var categoryName = categoryNameGen.Get.Trim();

        if (categoryName.Length < 2 || categoryName.Length > 100)
        {
            return; // Skip invalid inputs
        }

        // Precondition: Limit tip count to 0-10 as specified in the task
        var tipCount = tipCountGen.Get % 11; // 0-10 tips

        // Arrange: Create a use case with mocked repositories
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var tipRepositoryMock = new Mock<ITipRepository>();
        var useCase = new DeleteCategoryUseCase(
            categoryRepositoryMock.Object,
            tipRepositoryMock.Object);

        // Create a category
        var categoryId = Guid.NewGuid();
        var categoryIdValueObject = CategoryId.Create(categoryId);
        var category = DomainCategory.Create(categoryName);

        // Create random number of tips (0-10) associated with the category
        var tips = new List<DomainTip>();
        for (int i = 0; i < tipCount; i++)
        {
            var tip = DomainTip.Create(
                TipTitle.Create($"Tip {i + 1} for {categoryName}"),
                TipDescription.Create($"Description for tip {i + 1}"),
                new[] { TipStep.Create(1, $"Step 1 for tip {i + 1}") },
                categoryIdValueObject);
            tips.Add(tip);
        }

        // Set up the mock to return the category when queried by its ID
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.IsAny<CategoryId>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Set up the mock to return the tips associated with the category
        tipRepositoryMock
            .Setup(x => x.GetByCategoryAsync(
                It.IsAny<CategoryId>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tips);

        // Track the category and tips that are updated
        DomainCategory? updatedCategory = null;
        var updatedTips = new List<DomainTip>();

        categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Callback<DomainCategory, CancellationToken>((c, _) => updatedCategory = c)
            .Returns(Task.CompletedTask);

        tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Callback<DomainTip, CancellationToken>((t, _) => updatedTips.Add(t))
            .Returns(Task.CompletedTask);

        // Act: Delete the category
        var result = await useCase.ExecuteAsync(categoryId);

        // Assert: Deletion should succeed
        result.IsSuccess.Should().BeTrue(
            $"deleting category '{categoryName}' with {tipCount} tips should succeed");

        // Assert: Category should be marked as deleted
        updatedCategory.Should().NotBeNull("category should be updated");
        updatedCategory!.IsDeleted.Should().BeTrue(
            "category should have IsDeleted=true after deletion");
        updatedCategory.DeletedAt.Should().NotBeNull(
            "category should have DeletedAt set after deletion");
        updatedCategory.DeletedAt.Should().BeCloseTo(
            DateTime.UtcNow,
            TimeSpan.FromSeconds(5),
            "category DeletedAt should be set to current timestamp");

        // Assert: All tips should be marked as deleted
        updatedTips.Should().HaveCount(tipCount,
            $"all {tipCount} tips should be updated");

        foreach (var tip in updatedTips)
        {
            tip.IsDeleted.Should().BeTrue(
                $"tip '{tip.Title.Value}' should have IsDeleted=true after category deletion");
            tip.DeletedAt.Should().NotBeNull(
                $"tip '{tip.Title.Value}' should have DeletedAt set after category deletion");
            tip.DeletedAt.Should().BeCloseTo(
                DateTime.UtcNow,
                TimeSpan.FromSeconds(5),
                $"tip '{tip.Title.Value}' DeletedAt should be set to current timestamp");
        }

        // Verify repository interactions
        categoryRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()),
            Times.Once(),
            "category should be retrieved once");

        tipRepositoryMock.Verify(
            x => x.GetByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()),
            Times.Once(),
            "tips should be retrieved once");

        categoryRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()),
            Times.Once(),
            "category should be updated once");

        tipRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()),
            Times.Exactly(tipCount),
            $"each of the {tipCount} tips should be updated exactly once");
    }

    /// <summary>
    /// Property: Deleting a category with zero tips should only mark the category as deleted,
    /// without attempting to update any tips.
    /// **Validates: Requirements 3.5, 3.6, 3.7**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-category-management")]
    [Trait("Property", "Property 8: Cascade soft-delete to associated tips")]
    public async Task CascadeSoftDelete_ShouldOnlyMarkCategory_WhenCategoryHasNoTips(
        NonEmptyString categoryNameGen)
    {
        // Precondition: Ensure category name meets minimum length requirements (2-100 characters after trimming)
        var categoryName = categoryNameGen.Get.Trim();

        if (categoryName.Length < 2 || categoryName.Length > 100)
        {
            return; // Skip invalid inputs
        }

        // Arrange: Create a use case with mocked repositories
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var tipRepositoryMock = new Mock<ITipRepository>();
        var useCase = new DeleteCategoryUseCase(
            categoryRepositoryMock.Object,
            tipRepositoryMock.Object);

        // Create a category with no tips
        var categoryId = Guid.NewGuid();
        var category = DomainCategory.Create(categoryName);

        // Set up the mock to return the category when queried by its ID
        categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(
                It.IsAny<CategoryId>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Set up the mock to return an empty list of tips
        tipRepositoryMock
            .Setup(x => x.GetByCategoryAsync(
                It.IsAny<CategoryId>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DomainTip>());

        // Track the category that is updated
        DomainCategory? updatedCategory = null;

        categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()))
            .Callback<DomainCategory, CancellationToken>((c, _) => updatedCategory = c)
            .Returns(Task.CompletedTask);

        tipRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: Delete the category
        var result = await useCase.ExecuteAsync(categoryId);

        // Assert: Deletion should succeed
        result.IsSuccess.Should().BeTrue(
            $"deleting category '{categoryName}' with no tips should succeed");

        // Assert: Category should be marked as deleted
        updatedCategory.Should().NotBeNull("category should be updated");
        updatedCategory!.IsDeleted.Should().BeTrue(
            "category should have IsDeleted=true after deletion");
        updatedCategory.DeletedAt.Should().NotBeNull(
            "category should have DeletedAt set after deletion");
        updatedCategory.DeletedAt.Should().BeCloseTo(
            DateTime.UtcNow,
            TimeSpan.FromSeconds(5),
            "category DeletedAt should be set to current timestamp");

        // Verify repository interactions
        categoryRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()),
            Times.Once(),
            "category should be retrieved once");

        tipRepositoryMock.Verify(
            x => x.GetByCategoryAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()),
            Times.Once(),
            "tips should be retrieved once even when there are none");

        categoryRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<DomainCategory>(), It.IsAny<CancellationToken>()),
            Times.Once(),
            "category should be updated once");

        tipRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<DomainTip>(), It.IsAny<CancellationToken>()),
            Times.Never(),
            "no tips should be updated when category has no tips");
    }
}
