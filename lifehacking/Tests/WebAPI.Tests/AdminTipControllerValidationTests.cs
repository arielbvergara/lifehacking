using System.Net;
using System.Net.Http.Json;
using Application.Dtos.Tip;
using FluentAssertions;
using Infrastructure.Tests;
using WebAPI.ErrorHandling;
using Xunit;

namespace WebAPI.Tests;

/// <summary>
/// Focused validation tests for AdminTipController.
/// Tests all validation boundary conditions for title, description, steps, tags, and video URL.
/// Uses Firestore emulator for data storage and CustomWebApplicationFactory for test infrastructure.
/// </summary>
public sealed class AdminTipControllerValidationTests : FirestoreWebApiTestBase
{
    private readonly HttpClient _adminClient;

    public AdminTipControllerValidationTests(CustomWebApplicationFactory factory) : base(factory)
    {
        // Set up HttpClient with admin authentication token
        _adminClient = Factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Add("X-Test-Only-ExternalId", "admin-user-id");
        _adminClient.DefaultRequestHeaders.Add("X-Test-Only-Role", "Admin");
    }

    #region Helper Methods

    private async Task<Guid> CreateTestCategoryAsync()
    {
        var categoryRepository = GetCategoryRepository();
        var category = TestDataFactory.CreateCategory("Test Category");
        await categoryRepository.AddAsync(category);
        return category.Id.Value;
    }

    private CreateTipRequest CreateValidTipRequest(Guid categoryId)
    {
        return new CreateTipRequest(
            Title: "Valid Tip Title",
            Description: "This is a valid tip description with enough content",
            Steps: new List<TipStepRequest>
            {
                new(1, "First step with enough content to be valid")
            },
            CategoryId: categoryId,
            Tags: new List<string> { "test" },
            VideoUrl: null
        );
    }

    #endregion

    #region CreateTip - Title Validation Boundary Tests

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { Title = "" };

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
        errorResponse.Errors["Title"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenTitleIsOnlyWhitespace()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { Title = "     " };

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
        errorResponse.Errors["Title"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenTitleIsFourCharacters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { Title = "Test" };

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
        errorResponse.Errors["Title"].Should().Contain(e => e.Contains("at least 5 characters"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenTitleIsExactlyFiveCharacters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { Title = "Tests" };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a title with exactly 5 characters should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Title.Should().Be("Tests");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenTitleIsExactly200Characters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var title = new string('A', 200);
        var request = CreateValidTipRequest(categoryId) with { Title = title };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a title with exactly 200 characters should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Title.Should().Be(title);
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenTitleIs201Characters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var title = new string('A', 201);
        var request = CreateValidTipRequest(categoryId) with { Title = title };

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
        errorResponse.Errors["Title"].Should().Contain(e => e.Contains("200 characters"));
    }

    #endregion

    #region CreateTip - Description Validation Boundary Tests

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenDescriptionIsEmpty()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { Description = "" };

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
        errorResponse.Errors["Description"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenDescriptionIsOnlyWhitespace()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { Description = "     " };

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
        errorResponse.Errors["Description"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenDescriptionIsNineCharacters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { Description = "123456789" };

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
    public async Task CreateTip_ShouldReturn201Created_WhenDescriptionIsExactlyTenCharacters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { Description = "1234567890" };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a description with exactly 10 characters should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Description.Should().Be("1234567890");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenDescriptionIsExactly2000Characters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var description = new string('B', 2000);
        var request = CreateValidTipRequest(categoryId) with { Description = description };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a description with exactly 2000 characters should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Description.Should().Be(description);
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenDescriptionIs2001Characters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var description = new string('B', 2001);
        var request = CreateValidTipRequest(categoryId) with { Description = description };

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
        errorResponse.Errors["Description"].Should().Contain(e => e.Contains("2000 characters"));
    }

    #endregion

    #region CreateTip - Step Validation Boundary Tests

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenStepsIsNull()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { Steps = null! };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        // Note: When Steps is null, ASP.NET model binding returns a framework-level validation error
        errorResponse.Errors.Should().ContainKey("Steps");
        errorResponse.Errors["Steps"].Should().Contain(e => e.Contains("required") || e.Contains("least one step"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenStepsIsEmpty()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { Steps = new List<TipStepRequest>() };

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
        errorResponse.Errors["Steps"].Should().Contain(e => e.Contains("least one step"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenStepDescriptionIsEmpty()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with
        {
            Steps = new List<TipStepRequest>
            {
                new(1, "")
            }
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Steps[0]");
        errorResponse.Errors["Steps[0]"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenStepDescriptionIsNineCharacters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with
        {
            Steps = new List<TipStepRequest>
            {
                new(1, "123456789")
            }
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Steps[0]");
        errorResponse.Errors["Steps[0]"].Should().Contain(e => e.Contains("at least 10 characters"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenStepDescriptionIsExactlyTenCharacters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with
        {
            Steps = new List<TipStepRequest>
            {
                new(1, "1234567890")
            }
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a step description with exactly 10 characters should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Steps[0].Description.Should().Be("1234567890");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenStepDescriptionIsExactly500Characters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var stepDescription = new string('C', 500);
        var request = CreateValidTipRequest(categoryId) with
        {
            Steps = new List<TipStepRequest>
            {
                new(1, stepDescription)
            }
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a step description with exactly 500 characters should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Steps[0].Description.Should().Be(stepDescription);
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenStepDescriptionIs501Characters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var stepDescription = new string('C', 501);
        var request = CreateValidTipRequest(categoryId) with
        {
            Steps = new List<TipStepRequest>
            {
                new(1, stepDescription)
            }
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Steps[0]");
        errorResponse.Errors["Steps[0]"].Should().Contain(e => e.Contains("500 characters"));
    }

    #endregion

    #region CreateTip - Tag Validation Boundary Tests

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenTagIsEmpty()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with
        {
            Tags = new List<string> { "" }
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Tags[0]");
        errorResponse.Errors["Tags[0]"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenTagIsOnlyWhitespace()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with
        {
            Tags = new List<string> { "   " }
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Tags[0]");
        errorResponse.Errors["Tags[0]"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenTagIsOneCharacter()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with
        {
            Tags = new List<string> { "A" }
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a tag with exactly 1 character should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Tags.Should().Contain("A");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenTagIsExactly50Characters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var tag = new string('D', 50);
        var request = CreateValidTipRequest(categoryId) with
        {
            Tags = new List<string> { tag }
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a tag with exactly 50 characters should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Tags.Should().Contain(tag);
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenTagIs51Characters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var tag = new string('D', 51);
        var request = CreateValidTipRequest(categoryId) with
        {
            Tags = new List<string> { tag }
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Tags[0]");
        errorResponse.Errors["Tags[0]"].Should().Contain(e => e.Contains("50 characters"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenTagsIsNull()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { Tags = null };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "tags are optional and null should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Tags.Should().BeEmpty();
    }

    #endregion

    #region CreateTip - Video URL Validation Tests

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenVideoUrlIsNull()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { VideoUrl = null };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "video URL is optional and null should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.VideoUrl.Should().BeNull();
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenVideoUrlIsEmptyString()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { VideoUrl = "" };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "an empty video URL string should be treated as null (no video URL provided)");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.VideoUrl.Should().BeNull();
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenVideoUrlIsInvalidFormat()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { VideoUrl = "not-a-valid-url" };

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
        errorResponse.Errors["VideoUrl"].Should().Contain(e => e.Contains("format is invalid"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenVideoUrlIsUnsupportedPlatform()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with { VideoUrl = "https://www.vimeo.com/123456" };

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
    public async Task CreateTip_ShouldReturn201Created_WhenVideoUrlIsValidYouTubeWatch()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with
        {
            VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a valid YouTube watch URL should be accepted");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.VideoUrl.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenVideoUrlIsValidYouTubeShorts()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with
        {
            VideoUrl = "https://www.youtube.com/shorts/abc123"
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a valid YouTube Shorts URL should be accepted");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.VideoUrl.Should().Be("https://www.youtube.com/shorts/abc123");
    }

    [Fact]
    public async Task CreateTip_ShouldReturn201Created_WhenVideoUrlIsValidInstagram()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with
        {
            VideoUrl = "https://www.instagram.com/p/abc123"
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a valid Instagram URL should be accepted");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.VideoUrl.Should().Be("https://www.instagram.com/p/abc123");
    }

    #endregion

    #region CreateTip - Multiple Field Validation Failures

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenMultipleFieldsAreInvalid()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = new CreateTipRequest(
            Title: "Bad",  // Too short (< 5 chars)
            Description: "Short",  // Too short (< 10 chars)
            Steps: new List<TipStepRequest>
            {
                new(1, "Bad")  // Too short (< 10 chars)
            },
            CategoryId: categoryId,
            Tags: new List<string> { new string('X', 51) },  // Too long (> 50 chars)
            VideoUrl: "not-a-url"  // Invalid format
        );

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

        // Verify all field errors are present
        errorResponse.Errors.Should().ContainKey("Title");
        errorResponse.Errors["Title"].Should().Contain(e => e.Contains("at least 5 characters"));

        errorResponse.Errors.Should().ContainKey("Description");
        errorResponse.Errors["Description"].Should().Contain(e => e.Contains("at least 10 characters"));

        errorResponse.Errors.Should().ContainKey("Steps[0]");
        errorResponse.Errors["Steps[0]"].Should().Contain(e => e.Contains("at least 10 characters"));

        errorResponse.Errors.Should().ContainKey("Tags[0]");
        errorResponse.Errors["Tags[0]"].Should().Contain(e => e.Contains("50 characters"));

        errorResponse.Errors.Should().ContainKey("VideoUrl");
        errorResponse.Errors["VideoUrl"].Should().Contain(e => e.Contains("format is invalid"));
    }

    [Fact]
    public async Task CreateTip_ShouldReturn400BadRequest_WhenTitleAndDescriptionAreInvalid()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var request = CreateValidTipRequest(categoryId) with
        {
            Title = "Bad",  // Too short
            Description = "Short"  // Too short
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/admin/tips", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();

        // Verify both field errors are present
        errorResponse.Errors.Should().ContainKey("Title");
        errorResponse.Errors["Title"].Should().Contain(e => e.Contains("at least 5 characters"));

        errorResponse.Errors.Should().ContainKey("Description");
        errorResponse.Errors["Description"].Should().Contain(e => e.Contains("at least 10 characters"));
    }

    #endregion

    #region UpdateTip - Title Validation Boundary Tests

    [Fact]
    public async Task UpdateTip_ShouldReturn400BadRequest_WhenTitleIsEmpty()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var tipRepository = GetTipRepository();
        var tip = TestDataFactory.CreateTip(Domain.ValueObject.CategoryId.Create(categoryId));
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "",
            Description: "Valid description with enough content",
            Steps: new List<TipStepRequest> { new(1, "Valid step description") },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Title");
        errorResponse.Errors["Title"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn400BadRequest_WhenTitleIsFourCharacters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var tipRepository = GetTipRepository();
        var tip = TestDataFactory.CreateTip(Domain.ValueObject.CategoryId.Create(categoryId));
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Test",
            Description: "Valid description with enough content",
            Steps: new List<TipStepRequest> { new(1, "Valid step description") },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Title");
        errorResponse.Errors["Title"].Should().Contain(e => e.Contains("at least 5 characters"));
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn200OK_WhenTitleIsExactlyFiveCharacters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var tipRepository = GetTipRepository();
        var tip = TestDataFactory.CreateTip(Domain.ValueObject.CategoryId.Create(categoryId));
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Tests",
            Description: "Valid description with enough content",
            Steps: new List<TipStepRequest> { new(1, "Valid step description") },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "a title with exactly 5 characters should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Title.Should().Be("Tests");
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn200OK_WhenTitleIsExactly200Characters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var tipRepository = GetTipRepository();
        var tip = TestDataFactory.CreateTip(Domain.ValueObject.CategoryId.Create(categoryId));
        await tipRepository.AddAsync(tip);

        var title = new string('A', 200);
        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: title,
            Description: "Valid description with enough content",
            Steps: new List<TipStepRequest> { new(1, "Valid step description") },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "a title with exactly 200 characters should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Title.Should().Be(title);
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn400BadRequest_WhenTitleIs201Characters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var tipRepository = GetTipRepository();
        var tip = TestDataFactory.CreateTip(Domain.ValueObject.CategoryId.Create(categoryId));
        await tipRepository.AddAsync(tip);

        var title = new string('A', 201);
        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: title,
            Description: "Valid description with enough content",
            Steps: new List<TipStepRequest> { new(1, "Valid step description") },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Title");
        errorResponse.Errors["Title"].Should().Contain(e => e.Contains("200 characters"));
    }

    #endregion

    #region UpdateTip - Description Validation Boundary Tests

    [Fact]
    public async Task UpdateTip_ShouldReturn400BadRequest_WhenDescriptionIsEmpty()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var tipRepository = GetTipRepository();
        var tip = TestDataFactory.CreateTip(Domain.ValueObject.CategoryId.Create(categoryId));
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Valid Title",
            Description: "",
            Steps: new List<TipStepRequest> { new(1, "Valid step description") },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be(400);
        errorResponse.Type.Should().Be(ErrorResponseTypes.ValidationErrorType);
        errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        errorResponse.Errors.Should().ContainKey("Description");
        errorResponse.Errors["Description"].Should().Contain(e => e.Contains("cannot be empty"));
    }

    [Fact]
    public async Task UpdateTip_ShouldReturn400BadRequest_WhenDescriptionIsNineCharacters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var tipRepository = GetTipRepository();
        var tip = TestDataFactory.CreateTip(Domain.ValueObject.CategoryId.Create(categoryId));
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Valid Title",
            Description: "123456789",
            Steps: new List<TipStepRequest> { new(1, "Valid step description") },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

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
    public async Task UpdateTip_ShouldReturn200OK_WhenDescriptionIsExactlyTenCharacters()
    {
        // Arrange
        var categoryId = await CreateTestCategoryAsync();
        var tipRepository = GetTipRepository();
        var tip = TestDataFactory.CreateTip(Domain.ValueObject.CategoryId.Create(categoryId));
        await tipRepository.AddAsync(tip);

        var request = new UpdateTipRequest(
            Id: tip.Id.Value,
            Title: "Valid Title",
            Description: "1234567890",
            Steps: new List<TipStepRequest> { new(1, "Valid step description") },
            CategoryId: categoryId,
            Tags: null,
            VideoUrl: null
        );

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/tips/{tip.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "a description with exactly 10 characters should be valid");

        var tipResponse = await response.Content.ReadFromJsonAsync<TipDetailResponse>();
        tipResponse.Should().NotBeNull();
        tipResponse!.Description.Should().Be("1234567890");
    }

    #endregion
}
