using System.Net;
using System.Net.Http.Json;
using Application.Dtos.Tip;
using Application.Interfaces;
using FluentAssertions;
using Infrastructure.Tests;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.ErrorHandling;
using Xunit;

namespace WebAPI.Tests;

/// <summary>
/// Integration tests for AdminTipController.
/// Tests admin-only tip management endpoints including authorization, validation, and security event logging.
/// Uses Firestore emulator for data storage and CustomWebApplicationFactory for test infrastructure.
/// </summary>
public sealed class AdminTipControllerIntegrationTests : FirestoreWebApiTestBase
{
    private readonly HttpClient _adminClient;
    private readonly HttpClient _nonAdminClient;
    private readonly HttpClient _unauthenticatedClient;
    private readonly TestSecurityEventNotifier _securityEventNotifier;

    public AdminTipControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
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

        // Get the test security event notifier for asserting security events
        using var scope = Factory.Services.CreateScope();
        _securityEventNotifier = (TestSecurityEventNotifier)scope.ServiceProvider.GetRequiredService<ISecurityEventNotifier>();
    }

    /// <summary>
    /// Helper method to create TipStepRequest objects from string descriptions.
    /// </summary>
    private static IReadOnlyList<TipStepRequest> CreateSteps(params string[] descriptions)
    {
        return descriptions.Select((desc, index) => new TipStepRequest(index + 1, desc)).ToList();
    }

    #region Authorization Tests

    [Fact]
    public async Task CreateTip_ShouldReturn403Forbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Test Tip",
            Description: "Test Description",
            Steps: CreateSteps("Step 1", "Step 2"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _nonAdminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-admin users should not be able to create tips");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Test Tip",
            Description: "Test Description",
            Steps: CreateSteps("Step 1", "Step 2"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _unauthenticatedClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "unauthenticated users should not be able to create tips");
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn403Forbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Original Title");
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Updated Title",
            Description: "Updated Description",
            Steps: CreateSteps("Step 1", "Step 2"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _nonAdminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-admin users should not be able to update tips");
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Original Title");
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Updated Title",
            Description: "Updated Description",
            Steps: CreateSteps("Step 1", "Step 2"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _unauthenticatedClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "unauthenticated users should not be able to update tips");
    }

    [Fact]
    public async Task DeleteTip_ShouldReturn403Forbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Test Tip");
        await tipRepository.AddAsync(tip);

        // Act
        var response = await _nonAdminClient.DeleteAsync($"/api/admin/tips/{tip.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-admin users should not be able to delete tips");
    }

    [Fact]
    public async Task DeleteTip_ShouldReturn401Unauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Test Tip");
        await tipRepository.AddAsync(tip);

        // Act
        var response = await _unauthenticatedClient.DeleteAsync($"/api/admin/tips/{tip.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "unauthenticated users should not be able to delete tips");
    }

    #endregion

    #region CreateTip Tests

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenRequestIsValid()
    {
        // Arrange
        _securityEventNotifier.ClearEvents();
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough content.",
            Steps: CreateSteps("Step 1: Do this", "Step 2: Do that"),
            CategoryId: category.Id.Value,
            Tags: new[] { "productivity", "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "creating a tip with valid data should return 201 Created");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Title.Should().Be("Valid Tip Title");
        tipResponse.Description.Should().Be("This is a valid tip description with enough content.");
        tipResponse.Steps.Should().HaveCount(2);
        tipResponse.CategoryId.Should().Be(category.Id.Value);
        tipResponse.Tags.Should().Contain(new[] { "productivity", "test" });
        tipResponse.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "",
            Description: "Valid description",
            Steps: CreateSteps("Step 1"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "creating a tip with an empty title should return 400 Bad Request");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("title", "error message should mention the title field");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenDescriptionIsEmpty()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Valid Title",
            Description: "",
            Steps: CreateSteps("Step 1"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "creating a tip with an empty description should return 400 Bad Request");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("description", "error message should mention the description field");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenStepsAreEmpty()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Valid Title",
            Description: "Valid description",
            Steps: Array.Empty<TipStepRequest>(),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "creating a tip with zero steps should return 400 Bad Request");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("step", "error message should mention steps");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn404NotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var nonExistentCategoryId = Guid.NewGuid();

        var request = new CreateTipRequest(
            Title: "Valid Title",
            Description: "Valid description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: nonExistentCategoryId,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "creating a tip with a non-existent category should return 404 Not Found");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Category", "error message should mention the category");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn404NotFound_WhenCategoryIsSoftDeleted()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Deleted Category");
        await categoryRepository.AddAsync(category);

        // Soft delete the category
        await categoryRepository.DeleteAsync(category.Id);

        var request = new CreateTipRequest(
            Title: "Valid Title",
            Description: "Valid description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "creating a tip with a soft-deleted category should return 404 Not Found");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Category", "error message should mention the category");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenVideoUrlIsInvalid()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Valid Title",
            Description: "Valid description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: "https://invalid-domain.com/video");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "creating a tip with an invalid video URL should return 400 Bad Request");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("URL", "error message should mention the URL");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenVideoUrlIsValid()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Tip with YouTube Video",
            Description: "This tip includes a YouTube video link.",
            Steps: CreateSteps("Step 1: Watch the video"),
            CategoryId: category.Id.Value,
            Tags: new[] { "video", "youtube" },
            VideoUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "creating a tip with a valid YouTube URL should return 201 Created");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.VideoUrl.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenInstagramUrlIsValid()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Tip with Instagram Video",
            Description: "This tip includes an Instagram video link.",
            Steps: CreateSteps("Step 1: Watch the video"),
            CategoryId: category.Id.Value,
            Tags: new[] { "video", "instagram" },
            VideoUrl: "https://www.instagram.com/p/ABC123xyz/");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "creating a tip with a valid Instagram URL should return 201 Created");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.VideoUrl.Should().Be("https://www.instagram.com/p/ABC123xyz/");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenYouTubeShortsUrlIsValid()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Tip with YouTube Shorts Video",
            Description: "This tip includes a YouTube Shorts video link.",
            Steps: CreateSteps("Step 1: Watch the short video"),
            CategoryId: category.Id.Value,
            Tags: new[] { "video", "shorts" },
            VideoUrl: "https://www.youtube.com/shorts/ABC123xyz");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "creating a tip with a valid YouTube Shorts URL should return 201 Created");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.VideoUrl.Should().Be("https://www.youtube.com/shorts/ABC123xyz");
    }

    [Fact]
    public async Task CreateTip_ShouldLogTipCreatedSecurityEvent_WhenTipIsCreated()
    {
        // Arrange
        _securityEventNotifier.ClearEvents();
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Test Tip for Security Event",
            Description: "This tip is created to test security event logging.",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var securityEvents = _securityEventNotifier.Events;
        securityEvents.Should().ContainSingle(e => e.EventName == SecurityEventNames.TipCreated,
            "a TipCreated security event should be logged when a tip is successfully created");

        var tipCreatedEvent = securityEvents.First(e => e.EventName == SecurityEventNames.TipCreated);
        tipCreatedEvent.Outcome.Should().Be(SecurityEventOutcomes.Success);
        tipCreatedEvent.SubjectId.Should().NotBeNullOrEmpty("the tip ID should be included in the security event");
    }

    #endregion

    #region Validation Error Response Structure Tests

    [Fact]
    public async Task CreateTip_ShouldReturnRFC7807ValidationError_WhenTitleIsInvalid()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Tip", // Too short
            Description: "Valid description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.Title.Should().Be(ErrorResponseTitles.ValidationErrorTitle);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Title");
        errorResponse.Errors["Title"].Should().Contain(e => e.Contains("at least 5 characters"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturnRFC7807ValidationError_WhenDescriptionIsInvalid()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Valid Title",
            Description: "Short", // Too short
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Description");
        errorResponse.Errors["Description"].Should().Contain(e => e.Contains("at least 10 characters"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturnRFC7807ValidationError_WhenStepsAreEmpty()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Valid Title",
            Description: "Valid description with enough characters",
            Steps: Array.Empty<TipStepRequest>(),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Steps");
        errorResponse.Errors["Steps"].Should().Contain(e => e.Contains("At least one step is required"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturnRFC7807ValidationError_WhenVideoUrlIsInvalid()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Valid Title",
            Description: "Valid description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: "https://invalid-domain.com/video");

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("VideoUrl");
        errorResponse.Errors["VideoUrl"].Should().Contain(e => e.Contains("supported platform"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturnRFC7807ValidationError_WhenMultipleFieldsAreInvalid()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var request = new CreateTipRequest(
            Title: "Tip", // Too short
            Description: "Short", // Too short
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: "https://invalid-domain.com/video"); // Invalid

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Title");
        errorResponse.Errors.Should().ContainKey("Description");
        errorResponse.Errors.Should().ContainKey("VideoUrl");
        errorResponse.Errors["Title"].Should().Contain(e => e.Contains("at least 5 characters"));
        errorResponse.Errors["Description"].Should().Contain(e => e.Contains("at least 10 characters"));
        errorResponse.Errors["VideoUrl"].Should().Contain(e => e.Contains("supported platform"));
    }

    #endregion

    #region Not-Found Error Response Structure Tests

    [Fact]
    public async Task CreateTip_ShouldReturnRFC7807NotFoundError_WhenCategoryDoesNotExist()
    {
        // Arrange
        var nonExistentCategoryId = Guid.NewGuid();

        var request = new CreateTipRequest(
            Title: "Valid Title",
            Description: "Valid description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: nonExistentCategoryId,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(404);
        errorResponse.Type.Should().Be(ErrorResponseTypes.NotFoundErrorType);
        errorResponse.Title.Should().Be(ErrorResponseTitles.NotFoundErrorTitle);
        errorResponse.Detail.Should().Contain("Category");
        errorResponse.Detail.Should().Contain(nonExistentCategoryId.ToString());
        errorResponse.Detail.Should().Contain("not found");
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateTip_ShouldReturnRFC7807NotFoundError_WhenTipDoesNotExist()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var nonExistentTipId = Guid.NewGuid();

        var request = new UpdateTipRequest(
            Id: nonExistentTipId,
            Title: "Updated Title",
            Description: "Updated description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{nonExistentTipId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(404);
        errorResponse.Type.Should().Be(ErrorResponseTypes.NotFoundErrorType);
        errorResponse.Title.Should().Be(ErrorResponseTitles.NotFoundErrorTitle);
        errorResponse.Detail.Should().Contain("Tip");
        errorResponse.Detail.Should().Contain(nonExistentTipId.ToString());
        errorResponse.Detail.Should().Contain("not found");
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteTip_ShouldReturnRFC7807NotFoundError_WhenTipDoesNotExist()
    {
        // Arrange
        var nonExistentTipId = Guid.NewGuid();

        // Act
        var response = await _adminClient.DeleteAsync($"/api/admin/tips/{nonExistentTipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(404);
        errorResponse.Type.Should().Be(ErrorResponseTypes.NotFoundErrorType);
        errorResponse.Title.Should().Be(ErrorResponseTitles.NotFoundErrorTitle);
        errorResponse.Detail.Should().Contain("Tip");
        errorResponse.Detail.Should().Contain(nonExistentTipId.ToString());
        errorResponse.Detail.Should().Contain("not found");
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateTip_ShouldReturnRFC7807NotFoundError_WhenCategoryIsSoftDeleted()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Deleted Category");
        await categoryRepository.AddAsync(category);

        // Soft delete the category
        await categoryRepository.DeleteAsync(category.Id);

        var request = new CreateTipRequest(
            Title: "Valid Title",
            Description: "Valid description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(404);
        errorResponse.Type.Should().Be(ErrorResponseTypes.NotFoundErrorType);
        errorResponse.Title.Should().Be(ErrorResponseTitles.NotFoundErrorTitle);
        errorResponse.Detail.Should().Contain("Category");
        errorResponse.Detail.Should().Contain(category.Id.Value.ToString());
        errorResponse.Detail.Should().Contain("not found");
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateTip_ShouldReturnRFC7807NotFoundError_WhenCategoryIsSoftDeleted()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category1 = TestDataFactory.CreateCategory("Active Category");
        var category2 = TestDataFactory.CreateCategory("Deleted Category");
        await categoryRepository.AddAsync(category1);
        await categoryRepository.AddAsync(category2);

        var tip = TestDataFactory.CreateTip(category1.Id, title: "Original Title");
        await tipRepository.AddAsync(tip);

        // Soft delete category2
        await categoryRepository.DeleteAsync(category2.Id);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Valid Title",
            Description: "Valid description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category2.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(404);
        errorResponse.Type.Should().Be(ErrorResponseTypes.NotFoundErrorType);
        errorResponse.Title.Should().Be(ErrorResponseTitles.NotFoundErrorTitle);
        errorResponse.Detail.Should().Contain("Category");
        errorResponse.Detail.Should().Contain(category2.Id.Value.ToString());
        errorResponse.Detail.Should().Contain("not found");
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region UpdateTip Tests

    [Fact]
    public async Task UpdateTip_ShouldReturn200OK_WhenRequestIsValid()
    {
        // Arrange
        _securityEventNotifier.ClearEvents();
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Original Title");
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Updated Title",
            Description: "Updated description with more details.",
            Steps: CreateSteps("Updated Step 1", "Updated Step 2", "Updated Step 3"),
            CategoryId: category.Id.Value,
            Tags: new[] { "updated", "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "updating a tip with valid data should return 200 OK");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Title.Should().Be("Updated Title");
        tipResponse.Description.Should().Be("Updated description with more details.");
        tipResponse.Steps.Should().HaveCount(3);
        tipResponse.Tags.Should().Contain(new[] { "updated", "test" });
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn404NotFound_WhenTipDoesNotExist()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var nonExistentTipId = Guid.NewGuid();

        var request = new UpdateTipRequest(
            Id: nonExistentTipId,
            Title: "Updated Title",
            Description: "Updated description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{nonExistentTipId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "updating a non-existent tip should return 404 Not Found");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found", "error message should indicate the tip was not found");
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn400BadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Original Title");
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "",
            Description: "Valid description",
            Steps: CreateSteps("Step 1"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "updating a tip with an empty title should return 400 Bad Request");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("title", "error message should mention the title field");
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn400BadRequest_WhenDescriptionIsEmpty()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Original Title");
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Valid Title",
            Description: "",
            Steps: CreateSteps("Step 1"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "updating a tip with an empty description should return 400 Bad Request");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("description", "error message should mention the description field");
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn400BadRequest_WhenStepsAreEmpty()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Original Title");
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Valid Title",
            Description: "Valid description",
            Steps: Array.Empty<TipStepRequest>(),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "updating a tip with zero steps should return 400 Bad Request");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("step", "error message should mention steps");
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn404NotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Original Title");
        await tipRepository.AddAsync(tip);

        var nonExistentCategoryId = Guid.NewGuid();

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Valid Title",
            Description: "Valid description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: nonExistentCategoryId,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "updating a tip with a non-existent category should return 404 Not Found");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Category", "error message should mention the category");
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn404NotFound_WhenCategoryIsSoftDeleted()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category1 = TestDataFactory.CreateCategory("Active Category");
        var category2 = TestDataFactory.CreateCategory("Deleted Category");
        await categoryRepository.AddAsync(category1);
        await categoryRepository.AddAsync(category2);

        var tip = TestDataFactory.CreateTip(category1.Id, title: "Original Title");
        await tipRepository.AddAsync(tip);

        // Soft delete category2
        await categoryRepository.DeleteAsync(category2.Id);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Valid Title",
            Description: "Valid description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category2.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "updating a tip to a soft-deleted category should return 404 Not Found");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Category", "error message should mention the category");
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn400BadRequest_WhenVideoUrlIsInvalid()
    {
        // Arrange
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Original Title");
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Valid Title",
            Description: "Valid description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: "https://invalid-domain.com/video");

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "updating a tip with an invalid video URL should return 400 Bad Request");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("URL", "error message should mention the URL");
    }

    [Fact]
    public async Task UpdateTip_ShouldLogTipUpdatedSecurityEvent_WhenTipIsUpdated()
    {
        // Arrange
        _securityEventNotifier.ClearEvents();
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Original Title");
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Updated Title for Security Event",
            Description: "Updated description with enough characters",
            Steps: CreateSteps("Step 1 with enough characters"),
            CategoryId: category.Id.Value,
            Tags: new[] { "test" },
            VideoUrl: null);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var securityEvents = _securityEventNotifier.Events;
        securityEvents.Should().ContainSingle(e => e.EventName == SecurityEventNames.TipUpdated,
            "a TipUpdated security event should be logged when a tip is successfully updated");

        var tipUpdatedEvent = securityEvents.First(e => e.EventName == SecurityEventNames.TipUpdated);
        tipUpdatedEvent.Outcome.Should().Be(SecurityEventOutcomes.Success);
        tipUpdatedEvent.SubjectId.Should().Be(tip.Id.Value.ToString(), "the tip ID should be included in the security event");
    }

    #endregion

    #region DeleteTip Tests

    [Fact]
    public async Task DeleteTip_ShouldReturn204NoContent_WhenTipExists()
    {
        // Arrange
        _securityEventNotifier.ClearEvents();
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Tip to Delete");
        await tipRepository.AddAsync(tip);

        // Act
        var response = await _adminClient.DeleteAsync($"/api/admin/tips/{tip.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "deleting an existing tip should return 204 No Content");

        // Verify the tip is soft-deleted
        var deletedTip = await tipRepository.GetByIdAsync(tip.Id);
        deletedTip.Should().BeNull("the tip should be soft-deleted and not returned by GetByIdAsync");
    }

    [Fact]
    public async Task DeleteTip_ShouldReturn404NotFound_WhenTipDoesNotExist()
    {
        // Arrange
        var nonExistentTipId = Guid.NewGuid();

        // Act
        var response = await _adminClient.DeleteAsync($"/api/admin/tips/{nonExistentTipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "deleting a non-existent tip should return 404 Not Found");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found", "error message should indicate the tip was not found");
    }

    [Fact]
    public async Task DeleteTip_ShouldLogTipDeletedSecurityEvent_WhenTipIsDeleted()
    {
        // Arrange
        _securityEventNotifier.ClearEvents();
        var categoryRepository = GetCategoryRepository();
        var tipRepository = GetTipRepository();

        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);

        var tip = TestDataFactory.CreateTip(category.Id, title: "Tip for Security Event");
        await tipRepository.AddAsync(tip);

        // Act
        var response = await _adminClient.DeleteAsync($"/api/admin/tips/{tip.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var securityEvents = _securityEventNotifier.Events;
        securityEvents.Should().ContainSingle(e => e.EventName == SecurityEventNames.TipDeleted,
            "a TipDeleted security event should be logged when a tip is successfully deleted");

        var tipDeletedEvent = securityEvents.First(e => e.EventName == SecurityEventNames.TipDeleted);
        tipDeletedEvent.Outcome.Should().Be(SecurityEventOutcomes.Success);
        tipDeletedEvent.SubjectId.Should().Be(tip.Id.Value.ToString(), "the tip ID should be included in the security event");
    }

    #endregion
}
