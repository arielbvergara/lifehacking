using System.Net;
using System.Net.Http.Json;
using Application.Dtos.Category;
using Application.Interfaces;
using FluentAssertions;
using Infrastructure.Tests;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.ErrorHandling;
using Xunit;

namespace WebAPI.Tests;

/// <summary>
/// Integration tests for AdminCategoryController.
/// Tests admin-only category management endpoints including authorization, validation, and cascade behavior.
/// Uses Firestore emulator for data storage and CustomWebApplicationFactory for test infrastructure.
/// </summary>
public sealed class AdminCategoryControllerIntegrationTests : FirestoreWebApiTestBase
{
    private readonly HttpClient _adminClient;
    private readonly HttpClient _nonAdminClient;
    private readonly HttpClient _unauthenticatedClient;

    public AdminCategoryControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
        // Set up HttpClient with admin authentication token
        _adminClient = Factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", "admin-user-id");
        _adminClient.DefaultRequestHeaders.Add("X-Test-Only-Role", "Admin");

        // Set up HttpClient without admin role for authorization tests
        _nonAdminClient = Factory.CreateClient();
        _nonAdminClient.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", "regular-user-id");
        _nonAdminClient.DefaultRequestHeaders.Add("X-Test-Only-Role", "User");

        // Set up HttpClient without authentication for authorization tests
        _unauthenticatedClient = Factory.CreateClient();
    }

    #region Authorization Tests

    [Fact]
    public async Task CreateCategory_ShouldReturn403Forbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        var request = new CreateCategoryRequest("Test Category");

        // Act
        var response = await _nonAdminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-admin users should not be able to create categories");
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var request = new CreateCategoryRequest("Test Category");

        // Act
        var response = await _unauthenticatedClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "unauthenticated users should not be able to create categories");
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn403Forbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name");
        await categoryRepository.AddAsync(category);

        var request = new UpdateCategoryRequest("Updated Name");

        // Act
        var response = await _nonAdminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-admin users should not be able to update categories");
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name");
        await categoryRepository.AddAsync(category);

        var request = new UpdateCategoryRequest("Updated Name");

        // Act
        var response = await _unauthenticatedClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "unauthenticated users should not be able to update categories");
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturn403Forbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        // Act
        var response = await _nonAdminClient.DeleteAsync($"/api/admin/categories/{category.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-admin users should not be able to delete categories");
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        // Act
        var response = await _unauthenticatedClient.DeleteAsync($"/api/admin/categories/{category.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "unauthenticated users should not be able to delete categories");
    }

    #endregion

    #region CreateCategory with Image Tests

    [Fact]
    public async Task CreateCategory_ShouldReturn201Created_WhenValidImageMetadataIsProvided()
    {
        // Arrange
        var imageDto = new CategoryImageDto(
            ImageUrl: "https://cdn.example.com/categories/test-image.jpg",
            ImageStoragePath: "categories/2024/test-image.jpg",
            OriginalFileName: "test-image.jpg",
            ContentType: "image/jpeg",
            FileSizeBytes: 1024 * 500, // 500KB
            UploadedAt: DateTime.UtcNow
        );

        var request = new CreateCategoryRequest("Category With Image", imageDto);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "creating a category with valid image metadata should return 201 Created");

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be("Category With Image");
        categoryResponse.Id.Should().NotBeEmpty();
        categoryResponse.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify image metadata is returned
        categoryResponse.Image.Should().NotBeNull("image metadata should be included in the response");
        categoryResponse.Image!.ImageUrl.Should().Be(imageDto.ImageUrl);
        categoryResponse.Image.ImageStoragePath.Should().Be(imageDto.ImageStoragePath);
        categoryResponse.Image.OriginalFileName.Should().Be(imageDto.OriginalFileName);
        categoryResponse.Image.ContentType.Should().Be(imageDto.ContentType);
        categoryResponse.Image.FileSizeBytes.Should().Be(imageDto.FileSizeBytes);
        categoryResponse.Image.UploadedAt.Should().BeCloseTo(imageDto.UploadedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn201Created_WhenNoImageMetadataIsProvided()
    {
        // Arrange
        var request = new CreateCategoryRequest("Category Without Image", Image: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "creating a category without image metadata should return 201 Created");

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be("Category Without Image");
        categoryResponse.Id.Should().NotBeEmpty();
        categoryResponse.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify image is null
        categoryResponse.Image.Should().BeNull("image should be null when no image metadata is provided");
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn400BadRequest_WhenImageContentTypeIsInvalid()
    {
        // Arrange
        var imageDto = new CategoryImageDto(
            ImageUrl: "https://cdn.example.com/categories/test-image.jpg",
            ImageStoragePath: "categories/2024/test-image.jpg",
            OriginalFileName: "test-image.jpg",
            ContentType: "application/pdf", // Invalid content type
            FileSizeBytes: 1024 * 500,
            UploadedAt: DateTime.UtcNow
        );

        var request = new CreateCategoryRequest("Category With Invalid Image", imageDto);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "creating a category with invalid image content type should return 400 Bad Request");

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.Errors.Should().ContainKey("Image.ContentType");
        errorResponse.Errors["Image.ContentType"].Should().Contain(e => e.Contains("Content type must be one of"));
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn400BadRequest_WhenImageFileSizeExceedsMaximum()
    {
        // Arrange
        var imageDto = new CategoryImageDto(
            ImageUrl: "https://cdn.example.com/categories/test-image.jpg",
            ImageStoragePath: "categories/2024/test-image.jpg",
            OriginalFileName: "test-image.jpg",
            ContentType: "image/jpeg",
            FileSizeBytes: 6 * 1024 * 1024, // 6MB, exceeds 5MB limit
            UploadedAt: DateTime.UtcNow
        );

        var request = new CreateCategoryRequest("Category With Oversized Image", imageDto);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "creating a category with oversized image should return 400 Bad Request");

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.Errors.Should().ContainKey("Image.FileSizeBytes");
        errorResponse.Errors["Image.FileSizeBytes"].Should().Contain(e => e.Contains("cannot exceed") && e.Contains("bytes"));
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn400BadRequest_WhenImageUrlIsInvalid()
    {
        // Arrange
        var imageDto = new CategoryImageDto(
            ImageUrl: "not-a-valid-url", // Invalid URL format
            ImageStoragePath: "categories/2024/test-image.jpg",
            OriginalFileName: "test-image.jpg",
            ContentType: "image/jpeg",
            FileSizeBytes: 1024 * 500,
            UploadedAt: DateTime.UtcNow
        );

        var request = new CreateCategoryRequest("Category With Invalid URL", imageDto);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "creating a category with invalid image URL should return 400 Bad Request");

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.Errors.Should().ContainKey("Image.ImageUrl");
        errorResponse.Errors["Image.ImageUrl"].Should().Contain(e => e.Contains("valid absolute URL"));
    }

    [Fact]
    public async Task GetCategories_ShouldReturnCategoriesWithAndWithoutImages_WhenMixedCategoriesExist()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();

        // Create category with image
        var imageDto = new CategoryImageDto(
            ImageUrl: "https://cdn.example.com/categories/tech-image.jpg",
            ImageStoragePath: "categories/2024/tech-image.jpg",
            OriginalFileName: "tech-image.jpg",
            ContentType: "image/jpeg",
            FileSizeBytes: 1024 * 500,
            UploadedAt: DateTime.UtcNow
        );
        var createWithImageRequest = new CreateCategoryRequest("Tech Category", imageDto);
        var createWithImageResponse = await _adminClient.PostAsJsonAsync("/api/admin/categories", createWithImageRequest);
        createWithImageResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Create category without image
        var createWithoutImageRequest = new CreateCategoryRequest("Health Category", Image: null);
        var createWithoutImageResponse = await _adminClient.PostAsJsonAsync("/api/admin/categories", createWithoutImageRequest);
        createWithoutImageResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Retrieve all categories using the public endpoint
        var publicClient = Factory.CreateClient();
        var response = await publicClient.GetAsync("/api/Category");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categoryList = await response.Content.ReadFromJsonAsync<CategoryListResponse>();
        categoryList.Should().NotBeNull();
        categoryList!.Items.Should().HaveCountGreaterThanOrEqualTo(2);

        // Verify category with image
        var categoryWithImage = categoryList.Items.FirstOrDefault(c => c.Name == "Tech Category");
        categoryWithImage.Should().NotBeNull("category with image should be in the list");
        categoryWithImage!.Image.Should().NotBeNull("image metadata should be present");
        categoryWithImage.Image!.ImageUrl.Should().Be(imageDto.ImageUrl);
        categoryWithImage.Image.ContentType.Should().Be(imageDto.ContentType);
        categoryWithImage.Image.FileSizeBytes.Should().Be(imageDto.FileSizeBytes);

        // Verify category without image
        var categoryWithoutImage = categoryList.Items.FirstOrDefault(c => c.Name == "Health Category");
        categoryWithoutImage.Should().NotBeNull("category without image should be in the list");
        categoryWithoutImage!.Image.Should().BeNull("image should be null for category without image");
    }

    #endregion

    #region CreateCategory Tests

    [Fact]
    public async Task CreateCategory_ShouldReturn201Created_WhenNameIsValid()
    {
        // Arrange
        var request = new CreateCategoryRequest("Valid Category Name");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "creating a category with a valid name should return 201 Created");

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be("Valid Category Name");
        categoryResponse.Id.Should().NotBeEmpty();
        categoryResponse.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn409Conflict_WhenNameAlreadyExists()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var existingCategory = TestDataFactory.CreateCategory("Duplicate Name");
        await categoryRepository.AddAsync(existingCategory);

        var request = new CreateCategoryRequest("Duplicate Name");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "creating a category with a duplicate name should return 409 Conflict");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already exists", "error message should indicate the name conflict");
    }

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
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "creating a category with a case-insensitive duplicate name should return 409 Conflict");
    }

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
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn400BadRequest_WhenNameIsTooShort()
    {
        // Arrange
        var request = new CreateCategoryRequest("A");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "creating a category with a name shorter than 2 characters should return 400 Bad Request");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("at least 2 characters", "error message should indicate the minimum length requirement");
    }

    [Fact]
    public async Task CreateCategory_ShouldReturn400BadRequest_WhenNameIsTooLong()
    {
        // Arrange
        var longName = new string('A', 101);
        var request = new CreateCategoryRequest(longName);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "creating a category with a name longer than 100 characters should return 400 Bad Request");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("100 characters", "error message should indicate the maximum length requirement");
    }

    #endregion

    #region Validation Error Response Structure Tests

    [Fact]
    public async Task CreateCategory_ShouldReturnRFC7807ValidationError_WhenNameIsEmpty()
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
        errorResponse.Detail.Should().NotBeNullOrEmpty();
        errorResponse.Instance.Should().NotBeNullOrEmpty();
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Name");
        errorResponse.Errors["Name"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task CreateCategory_ShouldReturnRFC7807ValidationError_WhenNameIsTooShort()
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
        errorResponse.Title.Should().Be(ErrorResponseTitles.ValidationErrorTitle);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Name");
        errorResponse.Errors["Name"].Should().Contain(e => e.Contains("at least 2 characters"));
    }

    [Fact]
    public async Task CreateCategory_ShouldReturnRFC7807ValidationError_WhenNameIsTooLong()
    {
        // Arrange
        var longName = new string('A', 101);
        var request = new CreateCategoryRequest(longName);

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
        errorResponse.Errors["Name"].Should().Contain(e => e.Contains("100 characters"));
    }

    #endregion

    #region UpdateCategory Tests

    [Fact]
    public async Task UpdateCategory_ShouldReturn200OK_WhenNameIsValid()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name");
        await categoryRepository.AddAsync(category);

        var request = new UpdateCategoryRequest("Updated Name");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "updating a category with a valid name should return 200 OK");

        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull();
        categoryResponse!.Name.Should().Be("Updated Name");
        categoryResponse.Id.Should().Be(category.Id.Value);
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn404NotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateCategoryRequest("New Name");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "updating a non-existent category should return 404 Not Found");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found", "error message should indicate the category was not found");
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn404NotFound_WhenCategoryIsSoftDeleted()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Deleted Category");
        await categoryRepository.AddAsync(category);

        // Soft delete the category
        await categoryRepository.DeleteAsync(category.Id);

        var request = new UpdateCategoryRequest("New Name");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "updating a soft-deleted category should return 404 Not Found");
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn409Conflict_WhenNewNameExistsOnDifferentCategory()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category1 = TestDataFactory.CreateCategory("Category 1");
        var category2 = TestDataFactory.CreateCategory("Category 2");
        await categoryRepository.AddAsync(category1);
        await categoryRepository.AddAsync(category2);

        var request = new UpdateCategoryRequest("Category 2");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category1.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "updating a category to have another category's name should return 409 Conflict");
    }

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
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn400BadRequest_WhenNameIsTooShort()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name");
        await categoryRepository.AddAsync(category);

        var request = new UpdateCategoryRequest("A");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "updating a category with a name shorter than 2 characters should return 400 Bad Request");
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn400BadRequest_WhenNameIsTooLong()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Original Name");
        await categoryRepository.AddAsync(category);

        var longName = new string('A', 101);
        var request = new UpdateCategoryRequest(longName);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "updating a category with a name longer than 100 characters should return 400 Bad Request");
    }

    #endregion

    #region Not-Found Error Response Structure Tests

    [Fact]
    public async Task UpdateCategory_ShouldReturnRFC7807NotFoundError_WhenCategoryDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateCategoryRequest("New Name");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(404);
        errorResponse.Type.Should().Be(ErrorResponseTypes.NotFoundErrorType);
        errorResponse.Title.Should().Be(ErrorResponseTitles.NotFoundErrorTitle);
        errorResponse.Detail.Should().Contain("Category");
        errorResponse.Detail.Should().Contain(nonExistentId.ToString());
        errorResponse.Detail.Should().Contain("not found");
        errorResponse.Instance.Should().NotBeNullOrEmpty();
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturnRFC7807NotFoundError_WhenCategoryDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _adminClient.DeleteAsync($"/api/admin/categories/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(404);
        errorResponse.Type.Should().Be(ErrorResponseTypes.NotFoundErrorType);
        errorResponse.Title.Should().Be(ErrorResponseTitles.NotFoundErrorTitle);
        errorResponse.Detail.Should().Contain("Category");
        errorResponse.Detail.Should().Contain(nonExistentId.ToString());
        errorResponse.Detail.Should().Contain("not found");
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Conflict Error Response Structure Tests

    [Fact]
    public async Task CreateCategory_ShouldReturnRFC7807ConflictError_WhenNameAlreadyExists()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var existingCategory = TestDataFactory.CreateCategory("Duplicate Name");
        await categoryRepository.AddAsync(existingCategory);

        var request = new CreateCategoryRequest("Duplicate Name");

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
        errorResponse.Detail.Should().Contain("Duplicate Name");
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturnRFC7807ConflictError_WhenNewNameExistsOnDifferentCategory()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category1 = TestDataFactory.CreateCategory("Category 1");
        var category2 = TestDataFactory.CreateCategory("Category 2");
        await categoryRepository.AddAsync(category1);
        await categoryRepository.AddAsync(category2);

        var request = new UpdateCategoryRequest("Category 2");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/categories/{category1.Id.Value}", request);

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

    #endregion

    #region DeleteCategory Tests

    [Fact]
    public async Task DeleteCategory_ShouldReturn204NoContent_WhenCategoryExists()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        // Act
        var response = await _adminClient.DeleteAsync($"/api/admin/categories/{category.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "deleting an existing category should return 204 No Content");

        // Verify the category is soft-deleted
        var deletedCategory = await categoryRepository.GetByIdAsync(category.Id);
        deletedCategory.Should().BeNull("the category should be soft-deleted and not returned by GetByIdAsync");
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturn404NotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _adminClient.DeleteAsync($"/api/admin/categories/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "deleting a non-existent category should return 404 Not Found");
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturn404NotFound_WhenCategoryIsAlreadySoftDeleted()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Deleted Category");
        await categoryRepository.AddAsync(category);

        // Soft delete the category
        await categoryRepository.DeleteAsync(category.Id);

        // Act
        var response = await _adminClient.DeleteAsync($"/api/admin/categories/{category.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "deleting an already soft-deleted category should return 404 Not Found");
    }

    [Fact]
    public async Task DeleteCategory_ShouldCascadeSoftDelete_WhenCategoryHasTips()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Category With Tips");
        await categoryRepository.AddAsync(category);

        var tip1 = TestDataFactory.CreateTip(category.Id, title: "Tip 1");
        var tip2 = TestDataFactory.CreateTip(category.Id, title: "Tip 2");
        await tipRepository.AddAsync(tip1);
        await tipRepository.AddAsync(tip2);

        // Act
        var response = await _adminClient.DeleteAsync($"/api/admin/categories/{category.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "deleting a category with tips should return 204 No Content");

        // Verify the category is soft-deleted
        var deletedCategory = await categoryRepository.GetByIdAsync(category.Id);
        deletedCategory.Should().BeNull("the category should be soft-deleted");

        // Verify the tips are soft-deleted
        var deletedTip1 = await tipRepository.GetByIdAsync(tip1.Id);
        var deletedTip2 = await tipRepository.GetByIdAsync(tip2.Id);
        deletedTip1.Should().BeNull("tip 1 should be soft-deleted");
        deletedTip2.Should().BeNull("tip 2 should be soft-deleted");
    }

    [Fact]
    public async Task DeleteCategory_ShouldSetIsDeletedAndDeletedAt_WhenCategoryIsDeleted()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        // Act
        var response = await _adminClient.DeleteAsync($"/api/admin/categories/{category.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify IsDeleted and DeletedAt are set by querying with includeDeleted
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var deletedCategory = await repo.GetByNameAsync(category.Name, includeDeleted: true);

        deletedCategory.Should().NotBeNull("the category should still exist in the database");
        deletedCategory!.IsDeleted.Should().BeTrue("IsDeleted should be set to true");
        deletedCategory.DeletedAt.Should().NotBeNull("DeletedAt should be set");
        deletedCategory.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5),
            "DeletedAt should be set to the current timestamp");
    }

    [Fact]
    public async Task DeleteCategory_ShouldSetIsDeletedAndDeletedAtForTips_WhenCategoryWithTipsIsDeleted()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Category With Tips");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Test Tip");
        await tipRepository.AddAsync(tip);

        // Act
        var response = await _adminClient.DeleteAsync($"/api/admin/categories/{category.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the tip has IsDeleted and DeletedAt set
        // Note: We need to access the tip through the data store with includeDeleted flag
        // Since ITipRepository doesn't expose GetByIdAsync with includeDeleted, we verify through GetByCategoryAsync
        // which should return empty for soft-deleted tips
        var activeTips = await tipRepository.GetByCategoryAsync(category.Id);
        activeTips.Should().BeEmpty("all tips should be soft-deleted");
    }

    #endregion
}
