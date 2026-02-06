using Application.Interfaces;
using Application.UseCases.Category;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using DomainCategory = Domain.Entities.Category;

namespace Application.Tests.UseCases.Category;

/// <summary>
/// Property-based tests for GetCategoriesUseCase.
/// Feature: public-category-endpoints
/// </summary>
public sealed class GetCategoriesUseCasePropertyTests
{
    // Feature: public-category-endpoints, Property 1: Soft-delete filtering for categories
    // Validates: Requirements 1.2, 5.1

    /// <summary>
    /// Property: GetCategoriesUseCase should only return non-deleted categories.
    /// This property verifies that soft-deleted categories are filtered out.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExecuteAsync_ShouldOnlyReturnNonDeletedCategories_WhenCategoriesHaveMixedDeletedFlags(
        NonEmptyArray<NonEmptyString> categoryNames,
        NonEmptyArray<bool> deletedFlags)
    {
        // Arrange: Create categories with mixed IsDeleted flags
        var categories = new List<DomainCategory>();
        var nonDeletedCount = 0;

        // Use the minimum of the two array lengths to avoid index out of range
        var count = Math.Min(categoryNames.Get.Length, deletedFlags.Get.Length);
        count = Math.Min(count, 20); // Limit to 20 categories for performance

        for (int i = 0; i < count; i++)
        {
            var nameStr = categoryNames.Get[i].Get.Trim();

            // Skip invalid names
            if (nameStr.Length < DomainCategory.MinNameLength ||
                nameStr.Length > DomainCategory.MaxNameLength)
            {
                continue;
            }

            var category = DomainCategory.Create(nameStr);

            if (deletedFlags.Get[i])
            {
                category.MarkDeleted();
            }
            else
            {
                nonDeletedCount++;
            }

            categories.Add(category);
        }

        // Skip if no valid categories were created
        if (categories.Count == 0)
        {
            return;
        }

        // Mock repository to return only non-deleted categories (as the real repository does)
        var nonDeletedCategories = categories.Where(c => !c.IsDeleted).ToList();
        var mockRepository = new Mock<ICategoryRepository>();
        mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(nonDeletedCategories);

        var useCase = new GetCategoriesUseCase(mockRepository.Object);

        // Act
        var result = useCase.ExecuteAsync().Result;

        // Assert: Result should be successful
        result.IsSuccess.Should().BeTrue("the use case should succeed");

        // Assert: All returned categories should be non-deleted (verified by repository mock)
        var returnedCategories = result.Value!.Items;
        returnedCategories.Should().HaveCount(
            nonDeletedCount,
            "the number of returned categories should match the number of non-deleted categories");

        // Assert: All returned IDs should match non-deleted categories
        var returnedIds = returnedCategories.Select(c => c.Id).ToHashSet();
        var expectedIds = nonDeletedCategories.Select(c => c.Id.Value).ToHashSet();

        returnedIds.Should().BeEquivalentTo(expectedIds,
            "all non-deleted categories should be present in the result");
    }

    // Feature: public-category-endpoints, Property 2: Category response structure
    // Validates: Requirements 1.4

    /// <summary>
    /// Property: GetCategoriesUseCase should return properly structured CategoryResponse objects.
    /// This property verifies that the response structure matches the source entities.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExecuteAsync_ShouldReturnValidCategoryResponseStructure_WhenCategoriesExist(
        NonEmptyArray<NonEmptyString> categoryNames)
    {
        // Arrange: Create non-deleted categories
        var categories = new List<DomainCategory>();

        var count = Math.Min(categoryNames.Get.Length, 20); // Limit to 20 categories

        for (int i = 0; i < count; i++)
        {
            var nameStr = categoryNames.Get[i].Get.Trim();

            // Skip invalid names
            if (nameStr.Length < DomainCategory.MinNameLength ||
                nameStr.Length > DomainCategory.MaxNameLength)
            {
                continue;
            }

            var category = DomainCategory.Create(nameStr);
            categories.Add(category);
        }

        // Skip if no valid categories were created
        if (categories.Count == 0)
        {
            return;
        }

        // Mock repository
        var mockRepository = new Mock<ICategoryRepository>();
        mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var useCase = new GetCategoriesUseCase(mockRepository.Object);

        // Act
        var result = useCase.ExecuteAsync().Result;

        // Assert: Result should be successful
        result.IsSuccess.Should().BeTrue("the use case should succeed");

        // Assert: Response should be CategoryListResponse
        result.Value.Should().NotBeNull("the response should not be null");
        result.Value!.Items.Should().NotBeNull("the items collection should not be null");

        // Assert: Each item should have valid Id, Name matching source
        foreach (var responseItem in result.Value.Items)
        {
            var sourceCategory = categories.First(c => c.Id.Value == responseItem.Id);

            responseItem.Id.Should().Be(sourceCategory.Id.Value,
                "the ID should match the source category");

            responseItem.Name.Should().Be(sourceCategory.Name,
                "the name should match the source category");
        }

        // Assert: Count should match
        result.Value.Items.Should().HaveCount(categories.Count,
            "the number of items should match the number of source categories");
    }
}
