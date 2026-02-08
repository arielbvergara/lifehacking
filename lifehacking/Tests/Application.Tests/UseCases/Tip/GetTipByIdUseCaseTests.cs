using Application.Dtos.Tip;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Tip;
using Domain.ValueObject;
using FluentAssertions;
using Moq;
using Xunit;
using DomainCategory = Domain.Entities.Category;
using DomainTip = Domain.Entities.Tip;

namespace Application.Tests.UseCases.Tip;

public class GetTipByIdUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnTipDetailResponse_WhenTipExists()
    {
        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new GetTipByIdUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var tipId = TipId.NewId();
        var categoryId = CategoryId.NewId();
        const string categoryName = "Test Category";
        const string tipTitle = "Test Tip";
        const string tipDescription = "This is a test tip description";

        var category = DomainCategory.FromPersistence(
            categoryId,
            categoryName,
            DateTime.UtcNow,
            null,
            false,
            null);

        var steps = new[]
        {
            TipStep.Create(1, "This is the first step with enough characters"),
            TipStep.Create(2, "This is the second step with enough characters")
        };

        var tags = new[]
        {
            Tag.Create("tag1"),
            Tag.Create("tag2")
        };

        var tip = DomainTip.FromPersistence(
            tipId,
            TipTitle.Create(tipTitle),
            TipDescription.Create(tipDescription),
            steps,
            categoryId,
            tags,
            null,
            DateTime.UtcNow,
            null,
            false,
            null);

        tipRepositoryMock
            .Setup(r => r.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tip);

        categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var request = new GetTipByIdRequest(tipId.Value);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(tipId.Value);
        result.Value.Title.Should().Be(tipTitle);
        result.Value.Description.Should().Be(tipDescription);
        result.Value.CategoryId.Should().Be(categoryId.Value);
        result.Value.CategoryName.Should().Be(categoryName);
        result.Value.Steps.Should().HaveCount(2);
        result.Value.Steps[0].StepNumber.Should().Be(1);
        result.Value.Steps[0].Description.Should().Be("This is the first step with enough characters");
        result.Value.Steps[1].StepNumber.Should().Be(2);
        result.Value.Steps[1].Description.Should().Be("This is the second step with enough characters");
        result.Value.Tags.Should().HaveCount(2);
        result.Value.Tags.Should().Contain("tag1");
        result.Value.Tags.Should().Contain("tag2");
        result.Value.YouTubeUrl.Should().BeNull();
        result.Value.YouTubeVideoId.Should().BeNull();

        tipRepositoryMock.Verify(
            r => r.GetByIdAsync(tipId, It.IsAny<CancellationToken>()),
            Times.Once);

        categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnTipDetailResponseWithYouTube_WhenTipHasYouTubeUrl()
    {
        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new GetTipByIdUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var tipId = TipId.NewId();
        var categoryId = CategoryId.NewId();
        const string categoryName = "Test Category";
        const string youtubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

        var category = DomainCategory.FromPersistence(
            categoryId,
            categoryName,
            DateTime.UtcNow,
            null,
            false,
            null);

        var steps = new[] { TipStep.Create(1, "Watch the video and follow along carefully") };
        var tags = new[] { Tag.Create("video") };

        var tip = DomainTip.FromPersistence(
            tipId,
            TipTitle.Create("Video Tip"),
            TipDescription.Create("A tip with a YouTube video"),
            steps,
            categoryId,
            tags,
            VideoUrl.Create(youtubeUrl),
            DateTime.UtcNow,
            null,
            false,
            null);

        tipRepositoryMock
            .Setup(r => r.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tip);

        categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var request = new GetTipByIdRequest(tipId.Value);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.YouTubeUrl.Should().Be(youtubeUrl);
        result.Value.YouTubeVideoId.Should().Be("dQw4w9WgXcQ");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundError_WhenTipDoesNotExist()
    {
        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new GetTipByIdUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var tipId = TipId.NewId();

        tipRepositoryMock
            .Setup(r => r.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainTip?)null);

        var request = new GetTipByIdRequest(tipId.Value);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Should().BeOfType<NotFoundException>();
        result.Error!.Message.Should().Contain($"Tip with ID '{tipId.Value}' was not found");

        tipRepositoryMock.Verify(
            r => r.GetByIdAsync(tipId, It.IsAny<CancellationToken>()),
            Times.Once);

        categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundError_WhenCategoryDoesNotExist()
    {
        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new GetTipByIdUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        var tipId = TipId.NewId();
        var categoryId = CategoryId.NewId();

        var steps = new[] { TipStep.Create(1, "This is a test step with enough characters") };
        var tags = new[] { Tag.Create("test") };

        var tip = DomainTip.FromPersistence(
            tipId,
            TipTitle.Create("Test Tip"),
            TipDescription.Create("Test description"),
            steps,
            categoryId,
            tags,
            null,
            DateTime.UtcNow,
            null,
            false,
            null);

        tipRepositoryMock
            .Setup(r => r.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tip);

        categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainCategory?)null);

        var request = new GetTipByIdRequest(tipId.Value);

        // Act
        var result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Should().BeOfType<NotFoundException>();
        result.Error!.Message.Should().Contain($"Category with ID '{categoryId.Value}' was not found");

        tipRepositoryMock.Verify(
            r => r.GetByIdAsync(tipId, It.IsAny<CancellationToken>()),
            Times.Once);

        categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var tipRepositoryMock = new Mock<ITipRepository>();
        var categoryRepositoryMock = new Mock<ICategoryRepository>();
        var useCase = new GetTipByIdUseCase(tipRepositoryMock.Object, categoryRepositoryMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            useCase.ExecuteAsync(null!, CancellationToken.None));

        tipRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<TipId>(), It.IsAny<CancellationToken>()),
            Times.Never);

        categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
