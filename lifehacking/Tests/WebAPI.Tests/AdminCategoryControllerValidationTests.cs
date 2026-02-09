using System.Net;
using System.Net.Http.Json;
using Application.Dtos.Category;
using FluentAssertions;
using Infrastructure.Tests;
using WebAPI.ErrorHandling;
using Xunit;

namespace WebAPI.Tests;

/// <summary>
/// Focused validation tests for AdminCategoryController.
/// Tests all validation boundary conditions, whitespace handling, and error response structure.
/// Uses Firestore emulator for data storage and CustomWebApplicationFactory for test infrastructure.
/// </summary>
public sealed class AdminCategoryControllerValidationTests : FirestoreWebApiTestBase
{
    private readonly HttpClient _adminClient;

    public AdminCategoryControllerValidationTests(CustomWebApplicationFactory factory) : base(factory)
    {
        // Set up HttpClient with admin authentication token
        _adminClient = Factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", "admin-user-id");
        _adminClient.DefaultRequestHeaders.Add("X-Test-Only-Role", "Admin");
    }

    #region CreateCategory - Name Length Boundary Tests

    [Fact]
    public async Task CreateCategory_ShouldReturn400BadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateCategoryRequest("");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.Title.Should().Be(ErrorResponseTitles.ValidationErrorTitle);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Name");
        errorResponse.Errors["Name"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn400BadRequest_WhenNameIsOnlyWhitespace()
    {
        // Arrange
        var request = new CreateCategoryRequest("   ");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Name");
        errorResponse.Errors["Name"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn400BadRequest_WhenNameIsOneCharacter()
    {
        // Arrange
        var request = new CreateCategoryRequest("A");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Name");
        errorResponse.Errors["Name"].Should().Contain(e => e.Contains("at least 2 characters"));
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn201Created_WhenNameIsExactlyTwoCharacters()
    {
        // Arrange
        var request = new CreateCategoryRequest("XY");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a name with exactly 2 characters should be valid");

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be("XY");
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn201Created_WhenNameIsExactly100Characters()
    {
        // Arrange
        var name = new string('B', 100);
        var request = new CreateCategoryRequest(name);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a name with exactly 100 characters should be valid");

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be(name);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn400BadRequest_WhenNameIs101Characters()
    {
        // Arrange
        var name = new string('A', 101);
        var request = new CreateCategoryRequest(name);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Name");
        errorResponse.Errors["Name"].Should().Contain(e => e.Contains("100 characters"));
    }

    #endregion

    #region CreateCategory - Whitespace Handling Tests

    [Fact]
    public async Task CreateCategory_ShouldTrimLeadingWhitespace_WhenNameHasLeadingSpaces()
    {
        // Arrange
        var request = new CreateCategoryRequest("   Leading Whitespace Test");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be("Leading Whitespace Test",
            "leading whitespace should be trimmed from the category name");
    }

    [Fact]
    public async Task CreateCategory_ShouldTrimTrailingWhitespace_WhenNameHasTrailingSpaces()
    {
        // Arrange
        var request = new CreateCategoryRequest("Trailing Whitespace Test   ");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be("Trailing Whitespace Test",
            "trailing whitespace should be trimmed from the category name");
    }

    [Fact]
    public async Task CreateCategory_ShouldTrimBothLeadingAndTrailingWhitespace_WhenNameHasBoth()
    {
        // Arrange
        var request = new CreateCategoryRequest("   Both Whitespace Test   ");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be("Both Whitespace Test",
            "both leading and trailing whitespace should be trimmed from the category name");
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn400BadRequest_WhenNameIsOneCharacterAfterTrimming()
    {
        // Arrange - "A" with spaces becomes "A" after trimming (1 character)
        var request = new CreateCategoryRequest("  A  ");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "a name that is only 1 character after trimming should be invalid");

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Errors.Should().ContainKey("Name");
        errorResponse.Errors["Name"].Should().Contain(e => e.Contains("at least 2 characters"));
    }

    #endregion

    #region CreateCategory - Case-Insensitive Uniqueness Tests

    [Fact]
    public async Task CreateCategory_ShouldReturn409Conflict_WhenNameExistsWithDifferentCasing()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var existingCategory = TestDataFactory.CreateCategory("Technology");
        await categoryRepository.AddAsync(existingCategory);

        var request = new CreateCategoryRequest("TECHNOLOGY");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(409);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ConflictErrorType);
        errorResponse.Title.Should().Be(ErrorResponseTitles.ConflictErrorTitle);
        errorResponse.Detail.Should().Contain("already exists");
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn409Conflict_WhenNameExistsWithMixedCasing()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var existingCategory = TestDataFactory.CreateCategory("Technology");
        await categoryRepository.AddAsync(existingCategory);

        var request = new CreateCategoryRequest("TeChnOloGy");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(409);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ConflictErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region CreateCategory - Soft-Deleted Category Name Reuse Prevention Tests

    [Fact]
    public async Task CreateCategory_ShouldReturn409Conflict_WhenSoftDeletedCategoryHasSameName()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Deleted Category");
        await categoryRepository.AddAsync(category);

        // Soft delete the category
        await categoryRepository.DeleteAsync(category.Id);

        var request = new CreateCategoryRequest("Deleted Category");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "creating a category with a soft-deleted category's name should return 409 Conflict");

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(409);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ConflictErrorType);
        errorResponse.Detail.Should().Contain("already exists");
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn409Conflict_WhenSoftDeletedCategoryHasSameNameWithDifferentCasing()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Deleted Category");
        await categoryRepository.AddAsync(category);

        // Soft delete the category
        await categoryRepository.DeleteAsync(category.Id);

        var request = new CreateCategoryRequest("DELETED CATEGORY");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "creating a category with a soft-deleted category's name (different casing) should return 409 Conflict");

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(409);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ConflictErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region UpdateCategory - Name Length Boundary Tests

    [Fact]
    public async Task UpdateCategory_ShouldReturn400BadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name");
        await categoryRepository.AddAsync(category);

        var request = new UpdateCategoryRequest("");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Name");
        errorResponse.Errors["Name"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn400BadRequest_WhenNameIsOnlyWhitespace()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name");
        await categoryRepository.AddAsync(category);

        var request = new UpdateCategoryRequest("   ");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Name");
        errorResponse.Errors["Name"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn400BadRequest_WhenNameIsOneCharacter()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name");
        await categoryRepository.AddAsync(category);

        var request = new UpdateCategoryRequest("A");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Name");
        errorResponse.Errors["Name"].Should().Contain(e => e.Contains("at least 2 characters"));
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn200OK_WhenNameIsExactlyTwoCharacters()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name For Two Char Test");
        await categoryRepository.AddAsync(category);

        var request = new UpdateCategoryRequest("XZ");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "a name with exactly 2 characters should be valid");

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be("XZ");
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn200OK_WhenNameIsExactly100Characters()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name For 100 Char Test");
        await categoryRepository.AddAsync(category);

        var name = new string('C', 100);
        var request = new UpdateCategoryRequest(name);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "a name with exactly 100 characters should be valid");

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be(name);
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn400BadRequest_WhenNameIs101Characters()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name");
        await categoryRepository.AddAsync(category);

        var name = new string('A', 101);
        var request = new UpdateCategoryRequest(name);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Name");
        errorResponse.Errors["Name"].Should().Contain(e => e.Contains("100 characters"));
    }

    #endregion

    #region UpdateCategory - Whitespace Handling Tests

    [Fact]
    public async Task UpdateCategory_ShouldTrimLeadingWhitespace_WhenNameHasLeadingSpaces()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name For Leading Test");
        await categoryRepository.AddAsync(category);

        var request = new UpdateCategoryRequest("   Updated Name With Leading");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be("Updated Name With Leading",
            "leading whitespace should be trimmed from the category name");
    }

    [Fact]
    public async Task UpdateCategory_ShouldTrimTrailingWhitespace_WhenNameHasTrailingSpaces()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name For Trailing Test");
        await categoryRepository.AddAsync(category);

        var request = new UpdateCategoryRequest("Updated Name With Trailing   ");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be("Updated Name With Trailing",
            "trailing whitespace should be trimmed from the category name");
    }

    [Fact]
    public async Task UpdateCategory_ShouldTrimBothLeadingAndTrailingWhitespace_WhenNameHasBoth()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name For Both Test");
        await categoryRepository.AddAsync(category);

        var request = new UpdateCategoryRequest("   Updated Name With Both   ");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be("Updated Name With Both",
            "both leading and trailing whitespace should be trimmed from the category name");
    }

    #endregion

    #region UpdateCategory - Case-Insensitive Uniqueness Tests

    [Fact]
    public async Task UpdateCategory_ShouldReturn409Conflict_WhenNewNameExistsOnDifferentCategoryWithDifferentCasing()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category1 = TestDataFactory.CreateCategory("Category 1");
        var category2 = TestDataFactory.CreateCategory("Category 2");
        await categoryRepository.AddAsync(category1);
        await categoryRepository.AddAsync(category2);

        var request = new UpdateCategoryRequest("CATEGORY 2");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category1.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(409);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ConflictErrorType);
        errorResponse.Detail.Should().Contain("already exists");
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn200OK_WhenUpdatingToSameNameWithDifferentCasing()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var uniqueName = $"Technology-{Guid.NewGuid():N}";
        var category = TestDataFactory.CreateCategory(uniqueName);
        await categoryRepository.AddAsync(category);

        var request = new UpdateCategoryRequest(uniqueName.ToUpperInvariant());

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "updating a category to its own name with different casing should be allowed");

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be(uniqueName.ToUpperInvariant());
    }

    #endregion

    #region UpdateCategory - Soft-Deleted Category Name Reuse Prevention Tests

    [Fact]
    public async Task UpdateCategory_ShouldReturn409Conflict_WhenNewNameMatchesSoftDeletedCategory()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var activeCategory = TestDataFactory.CreateCategory("Active Category");
        var deletedCategory = TestDataFactory.CreateCategory("Deleted Category");
        await categoryRepository.AddAsync(activeCategory);
        await categoryRepository.AddAsync(deletedCategory);

        // Soft delete the second category
        await categoryRepository.DeleteAsync(deletedCategory.Id);

        var request = new UpdateCategoryRequest("Deleted Category");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{activeCategory.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "updating a category to have a soft-deleted category's name should return 409 Conflict");

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(409);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ConflictErrorType);
        errorResponse.Detail.Should().Contain("already exists");
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn409Conflict_WhenNewNameMatchesSoftDeletedCategoryWithDifferentCasing()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var activeCategory = TestDataFactory.CreateCategory("Active Category");
        var deletedCategory = TestDataFactory.CreateCategory("Deleted Category");
        await categoryRepository.AddAsync(activeCategory);
        await categoryRepository.AddAsync(deletedCategory);

        // Soft delete the second category
        await categoryRepository.DeleteAsync(deletedCategory.Id);

        var request = new UpdateCategoryRequest("DELETED CATEGORY");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{activeCategory.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "updating a category to have a soft-deleted category's name (different casing) should return 409 Conflict");

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(409);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ConflictErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    #endregion
}
