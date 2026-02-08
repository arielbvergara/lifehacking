using System.Net;
using System.Net.Http.Json;
using Application.Dtos.Category;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace WebAPI.Tests;

/// <summary>
/// Property-based tests for AdminCategoryController.
/// Feature: admin-category-management
/// 
/// These tests verify universal properties that should hold across all valid inputs
/// using FsCheck to generate random test data and run 100+ iterations per property.
/// </summary>
public sealed class AdminCategoryControllerPropertyTests : FirestoreWebApiTestBase
{
    private readonly HttpClient _adminClient;

    public AdminCategoryControllerPropertyTests(CustomWebApplicationFactory factory) : base(factory)
    {
        // Set up HttpClient with admin authentication token
        _adminClient = Factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", "admin-user-id");
        _adminClient.DefaultRequestHeaders.Add("X-Test-Only-Role", "Admin");
    }

    // Feature: admin-category-management, Property 1: Valid category creation succeeds
    // For any valid category name (2-100 characters), creating a category through the admin endpoint
    // should succeed and return the created category with HTTP 201.
    // Validates: Requirements 1.1

    /// <summary>
    /// Property: POST /api/admin/categories should return 201 Created with the category details
    /// for any valid category name (2-100 characters).
    /// This property verifies that category creation succeeds across all valid inputs.
    /// **Validates: Requirements 1.1**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "admin-category-management")]
    [Trait("Property", "Property 1: Valid category creation succeeds")]
    public async Task CreateCategory_ShouldReturnCreated_ForAnyValidCategoryName(
        NonEmptyString nameGen)
    {
        // Arrange: Generate a valid category name (2-100 characters)
        // FsCheck generates random strings, so we need to constrain them to valid lengths
        var baseName = nameGen.Get.Trim();

        // Skip if the generated name is too short or too long
        if (baseName.Length < 2 || baseName.Length > 100)
        {
            return; // Skip invalid inputs
        }

        // Make the name unique to avoid conflicts with other test iterations
        var categoryName = $"{baseName}_{Guid.NewGuid():N}";

        // Ensure the unique name still meets length constraints
        if (categoryName.Length > 100)
        {
            categoryName = categoryName.Substring(0, 100);
        }

        var request = new CreateCategoryRequest(categoryName);

        // Act: Create the category via the admin endpoint
        var response = await _adminClient.PostAsJsonAsync("/api/admin/categories", request);

        // Assert: Response should be 201 Created
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"creating category with valid name '{categoryName}' (length: {categoryName.Length}) should return 201 Created");

        // Assert: Response should contain the created category details
        var categoryResponse = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        categoryResponse.Should().NotBeNull(
            "response should deserialize to CategoryResponse");

        categoryResponse!.Name.Should().Be(categoryName,
            "returned category name should match the requested name");

        categoryResponse.Id.Should().NotBeEmpty(
            "returned category should have a valid ID");

        categoryResponse.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1),
            "returned category should have a recent creation timestamp");

        // Assert: Verify the category was actually created in the repository
        var categoryRepository = GetCategoryRepository();
        var categoryId = Domain.ValueObject.CategoryId.Create(categoryResponse.Id);
        var createdCategory = await categoryRepository.GetByIdAsync(categoryId);

        createdCategory.Should().NotBeNull(
            "created category should exist in the repository");

        createdCategory!.Name.Should().Be(categoryName,
            "category in repository should have the correct name");

        createdCategory.IsDeleted.Should().BeFalse(
            "newly created category should not be soft-deleted");

        createdCategory.DeletedAt.Should().BeNull(
            "newly created category should not have a deletion timestamp");
    }
}
