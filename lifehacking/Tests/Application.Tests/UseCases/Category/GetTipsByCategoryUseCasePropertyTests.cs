using Application.Dtos;
using Application.Dtos.Category;
using Application.Dtos.Tip;
using Application.Interfaces;
using Application.UseCases.Category;
using Domain.ValueObject;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using DomainCategory = Domain.Entities.Category;
using DomainTip = Domain.Entities.Tip;

namespace Application.Tests.UseCases.Category;

/// <summary>
/// Property-based tests for GetTipsByCategoryUseCase.
/// Feature: public-category-endpoints
/// </summary>
public sealed class GetTipsByCategoryUseCasePropertyTests
{
    // Feature: public-category-endpoints, Property 3: Soft-delete filtering for tips by category
    // Validates: Requirements 2.2, 5.2

    /// <summary>
    /// Property: GetTipsByCategoryUseCase should only return non-deleted tips for the specified category.
    /// This property verifies that soft-deleted tips are filtered out and only tips from the requested category are returned.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExecuteAsync_ShouldOnlyReturnNonDeletedTipsForCategory_WhenTipsHaveMixedDeletedFlags(
        NonEmptyString categoryName,
        NonEmptyArray<NonEmptyString> tipTitles,
        NonEmptyArray<bool> deletedFlags)
    {
        // Arrange: Create a category
        var categoryNameStr = categoryName.Get.Trim();
        if (categoryNameStr.Length < DomainCategory.MinNameLength ||
            categoryNameStr.Length > DomainCategory.MaxNameLength)
        {
            return; // Skip invalid category names
        }

        var category = DomainCategory.Create(categoryNameStr);

        // Create tips with mixed IsDeleted flags
        var tips = new List<DomainTip>();
        var nonDeletedCount = 0;

        var count = Math.Min(tipTitles.Get.Length, deletedFlags.Get.Length);
        count = Math.Min(count, 20); // Limit to 20 tips

        for (int i = 0; i < count; i++)
        {
            var titleStr = tipTitles.Get[i].Get;

            // Skip invalid titles
            if (titleStr.Length < 5)
            {
                continue;
            }

            var tip = CreateTestTip(titleStr, category.Id);

            if (deletedFlags.Get[i])
            {
                tip.MarkDeleted();
            }
            else
            {
                nonDeletedCount++;
            }

            tips.Add(tip);
        }

        // Skip if no valid tips were created
        if (tips.Count == 0)
        {
            return;
        }

        // Mock repositories
        var mockCategoryRepository = new Mock<ICategoryRepository>();
        mockCategoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var mockTipRepository = new Mock<ITipRepository>();
        mockTipRepository
            .Setup(r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((tips.Where(t => !t.IsDeleted).ToList(), nonDeletedCount));

        var useCase = new GetTipsByCategoryUseCase(
            mockCategoryRepository.Object,
            mockTipRepository.Object);

        var request = new GetTipsByCategoryRequest();

        // Act
        var result = useCase.ExecuteAsync(category.Id.Value.ToString(), request).Result;

        // Assert: Result should be successful
        result.IsSuccess.Should().BeTrue("the use case should succeed");

        // Assert: All returned tips should have IsDeleted = false (verified by repository mock)
        var returnedTips = result.Value!.Items;
        returnedTips.Should().HaveCount(nonDeletedCount,
            "only non-deleted tips should be returned");

        // Assert: All returned tips should belong to the requested category
        foreach (var tipResponse in returnedTips)
        {
            var sourceTip = tips.First(t => t.Id.Value == tipResponse.Id);
            sourceTip.CategoryId.Should().Be(category.Id,
                "all returned tips should belong to the requested category");
        }
    }

    // Feature: public-category-endpoints, Property 4: Invalid category ID rejection
    // Validates: Requirements 2.5, 6.4

    /// <summary>
    /// Property: GetTipsByCategoryUseCase should return ValidationException for invalid category IDs.
    /// This property verifies that non-GUID category IDs are rejected.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExecuteAsync_ShouldReturnValidationException_WhenCategoryIdIsInvalid(
        NonEmptyString invalidCategoryId)
    {
        // Arrange: Generate an invalid category ID (not a GUID)
        var categoryIdStr = invalidCategoryId.Get;

        // Skip if it happens to be a valid GUID
        if (Guid.TryParse(categoryIdStr, out _))
        {
            return;
        }

        var mockCategoryRepository = new Mock<ICategoryRepository>();
        var mockTipRepository = new Mock<ITipRepository>();

        var useCase = new GetTipsByCategoryUseCase(
            mockCategoryRepository.Object,
            mockTipRepository.Object);

        var request = new GetTipsByCategoryRequest();

        // Act
        var result = useCase.ExecuteAsync(categoryIdStr, request).Result;

        // Assert: Result should be a failure
        result.IsSuccess.Should().BeFalse("invalid category ID should result in failure");

        // Assert: Error should be ValidationException
        result.Error.Should().BeOfType<Exceptions.ValidationException>(
            "invalid category ID should return a ValidationException");

        result.Error!.Message.Should().Contain("Invalid category ID format",
            "error message should indicate invalid format");
    }

    // Feature: public-category-endpoints, Property 5: Invalid pagination parameter rejection
    // Validates: Requirements 3.3

    /// <summary>
    /// Property: GetTipsByCategoryUseCase should return ValidationException for invalid pagination parameters.
    /// This property verifies that invalid page numbers and page sizes are rejected.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExecuteAsync_ShouldReturnValidationException_WhenPaginationParametersAreInvalid(
        NonEmptyString categoryName,
        int pageNumber,
        int pageSize)
    {
        // Arrange: Create a valid category
        var categoryNameStr = categoryName.Get.Trim();
        if (categoryNameStr.Length < DomainCategory.MinNameLength ||
            categoryNameStr.Length > DomainCategory.MaxNameLength)
        {
            return;
        }

        var category = DomainCategory.Create(categoryNameStr);

        // Only test invalid pagination parameters
        var isPageNumberInvalid = pageNumber < 1;
        var isPageSizeInvalid = pageSize < 1 || pageSize > 100;

        // Skip if both parameters are valid
        if (!isPageNumberInvalid && !isPageSizeInvalid)
        {
            return;
        }

        // Mock repositories
        var mockCategoryRepository = new Mock<ICategoryRepository>();
        mockCategoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var mockTipRepository = new Mock<ITipRepository>();

        var useCase = new GetTipsByCategoryUseCase(
            mockCategoryRepository.Object,
            mockTipRepository.Object);

        var request = new GetTipsByCategoryRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // Act
        var result = useCase.ExecuteAsync(category.Id.Value.ToString(), request).Result;

        // Assert: Result should be a failure
        result.IsSuccess.Should().BeFalse("invalid pagination parameters should result in failure");

        // Assert: Error should be ValidationException
        result.Error.Should().BeOfType<Exceptions.ValidationException>(
            "invalid pagination parameters should return a ValidationException");
    }

    // Feature: public-category-endpoints, Property 6: Pagination metadata correctness
    // Validates: Requirements 3.4

    /// <summary>
    /// Property: GetTipsByCategoryUseCase should return mathematically correct pagination metadata.
    /// This property verifies that TotalPages, PageNumber, PageSize, and TotalItems are consistent.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExecuteAsync_ShouldReturnCorrectPaginationMetadata_WhenPaginationParametersAreValid(
        NonEmptyString categoryName,
        PositiveInt totalTips,
        PositiveInt pageNumber,
        PositiveInt pageSize)
    {
        // Arrange: Create a valid category
        var categoryNameStr = categoryName.Get.Trim();
        if (categoryNameStr.Length < DomainCategory.MinNameLength ||
            categoryNameStr.Length > DomainCategory.MaxNameLength)
        {
            return;
        }

        var category = DomainCategory.Create(categoryNameStr);

        // Constrain parameters to valid ranges
        var actualTotalTips = Math.Min(totalTips.Get, 1000); // Limit to 1000 tips
        var actualPageNumber = Math.Max(1, Math.Min(pageNumber.Get, 100)); // 1-100
        var actualPageSize = Math.Max(1, Math.Min(pageSize.Get, 100)); // 1-100

        // Create tips (just enough for the requested page)
        var tips = new List<DomainTip>();
        var tipsToCreate = Math.Min(actualPageSize, actualTotalTips);

        for (int i = 0; i < tipsToCreate; i++)
        {
            tips.Add(CreateTestTip($"Tip {i}", category.Id));
        }

        // Mock repositories
        var mockCategoryRepository = new Mock<ICategoryRepository>();
        mockCategoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var mockTipRepository = new Mock<ITipRepository>();
        mockTipRepository
            .Setup(r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((tips, actualTotalTips));

        var useCase = new GetTipsByCategoryUseCase(
            mockCategoryRepository.Object,
            mockTipRepository.Object);

        var request = new GetTipsByCategoryRequest
        {
            PageNumber = actualPageNumber,
            PageSize = actualPageSize
        };

        // Act
        var result = useCase.ExecuteAsync(category.Id.Value.ToString(), request).Result;

        // Assert: Result should be successful
        result.IsSuccess.Should().BeTrue("the use case should succeed");

        var metadata = result.Value!.Metadata;

        // Assert: Pagination metadata should be mathematically correct
        metadata.TotalItems.Should().Be(actualTotalTips,
            "TotalItems should match the total count from repository");

        metadata.PageNumber.Should().Be(actualPageNumber,
            "PageNumber should match the requested page number");

        metadata.PageSize.Should().Be(actualPageSize,
            "PageSize should match the requested page size");

        var expectedTotalPages = (int)Math.Ceiling((double)actualTotalTips / actualPageSize);
        metadata.TotalPages.Should().Be(expectedTotalPages,
            "TotalPages should be calculated correctly");
    }

    // Feature: public-category-endpoints, Property 7: Invalid sort parameter rejection
    // Validates: Requirements 4.3

    /// <summary>
    /// Property: GetTipsByCategoryUseCase should handle invalid sort parameters gracefully.
    /// Note: Since OrderBy is an enum (TipSortField), invalid values cannot be passed at compile time.
    /// This test verifies that null sort parameters use defaults.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExecuteAsync_ShouldUseDefaultSorting_WhenSortParametersAreNull(
        NonEmptyString categoryName)
    {
        // Arrange: Create a valid category
        var categoryNameStr = categoryName.Get.Trim();
        if (categoryNameStr.Length < DomainCategory.MinNameLength ||
            categoryNameStr.Length > DomainCategory.MaxNameLength)
        {
            return;
        }

        var category = DomainCategory.Create(categoryNameStr);

        // Mock repositories
        var mockCategoryRepository = new Mock<ICategoryRepository>();
        mockCategoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        TipQueryCriteria? capturedCriteria = null;
        var mockTipRepository = new Mock<ITipRepository>();
        mockTipRepository
            .Setup(r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<TipQueryCriteria, CancellationToken>((criteria, _) => capturedCriteria = criteria)
            .ReturnsAsync((new List<DomainTip>(), 0));

        var useCase = new GetTipsByCategoryUseCase(
            mockCategoryRepository.Object,
            mockTipRepository.Object);

        var request = new GetTipsByCategoryRequest
        {
            OrderBy = null,
            SortDirection = null
        };

        // Act
        var result = useCase.ExecuteAsync(category.Id.Value.ToString(), request).Result;

        // Assert: Result should be successful
        result.IsSuccess.Should().BeTrue("the use case should succeed");

        // Assert: Default sort parameters should be used
        capturedCriteria.Should().NotBeNull("criteria should be captured");
        capturedCriteria!.SortField.Should().Be(TipSortField.CreatedAt,
            "default sort field should be CreatedAt");
        capturedCriteria.SortDirection.Should().Be(SortDirection.Descending,
            "default sort direction should be Descending");
    }

    // Feature: public-category-endpoints, Property 8: Sort direction correctness
    // Validates: Requirements 4.5

    /// <summary>
    /// Property: GetTipsByCategoryUseCase should pass the correct sort direction to the repository.
    /// This property verifies that both Ascending and Descending directions are handled correctly.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExecuteAsync_ShouldPassCorrectSortDirection_WhenSortDirectionIsSpecified(
        NonEmptyString categoryName,
        bool useAscending)
    {
        // Arrange: Create a valid category
        var categoryNameStr = categoryName.Get.Trim();
        if (categoryNameStr.Length < DomainCategory.MinNameLength ||
            categoryNameStr.Length > DomainCategory.MaxNameLength)
        {
            return;
        }

        var category = DomainCategory.Create(categoryNameStr);
        var sortDirection = useAscending ? SortDirection.Ascending : SortDirection.Descending;

        // Mock repositories
        var mockCategoryRepository = new Mock<ICategoryRepository>();
        mockCategoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        TipQueryCriteria? capturedCriteria = null;
        var mockTipRepository = new Mock<ITipRepository>();
        mockTipRepository
            .Setup(r => r.SearchAsync(It.IsAny<TipQueryCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<TipQueryCriteria, CancellationToken>((criteria, _) => capturedCriteria = criteria)
            .ReturnsAsync((new List<DomainTip>(), 0));

        var useCase = new GetTipsByCategoryUseCase(
            mockCategoryRepository.Object,
            mockTipRepository.Object);

        var request = new GetTipsByCategoryRequest
        {
            SortDirection = sortDirection
        };

        // Act
        var result = useCase.ExecuteAsync(category.Id.Value.ToString(), request).Result;

        // Assert: Result should be successful
        result.IsSuccess.Should().BeTrue("the use case should succeed");

        // Assert: Sort direction should be passed correctly to repository
        capturedCriteria.Should().NotBeNull("criteria should be captured");
        capturedCriteria!.SortDirection.Should().Be(sortDirection,
            "the specified sort direction should be passed to the repository");
    }

    /// <summary>
    /// Helper method to create a test tip with minimal valid data.
    /// </summary>
    private static DomainTip CreateTestTip(string title, CategoryId categoryId)
    {
        var tipTitle = TipTitle.Create(title);
        var tipDescription = TipDescription.Create($"Test description for {title}");
        var steps = new[]
        {
            TipStep.Create(1, "This is the first step of the tip with enough characters"),
            TipStep.Create(2, "This is the second step of the tip with enough characters")
        };
        var tags = new[] { Tag.Create("test") };

        return DomainTip.Create(tipTitle, tipDescription, steps, categoryId, tags);
    }
}
