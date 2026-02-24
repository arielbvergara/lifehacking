using Application.Dtos;
using Application.Dtos.Favorite;
using Application.Dtos.Tip;
using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Favorite;
using Domain.ValueObject;
using FluentAssertions;
using Moq;
using Xunit;
using DomainCategory = Domain.Entities.Category;
using DomainTip = Domain.Entities.Tip;
using DomainUser = Domain.Entities.User;

namespace Application.Tests.UseCases.Favorite;

public class SearchUserFavoritesUseCaseTests
{
    private readonly Mock<IFavoritesRepository> _favoritesRepositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly SearchUserFavoritesUseCase _useCase;

    public SearchUserFavoritesUseCaseTests()
    {
        _favoritesRepositoryMock = new Mock<IFavoritesRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _useCase = new SearchUserFavoritesUseCase(
            _favoritesRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPagedFavoritesResponse_WhenFavoritesExist()
    {
        // Arrange
        var userId = UserId.NewId();
        var categoryId = CategoryId.NewId();
        const string categoryName = "Test Category";

        var user = DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("auth123"));

        var category = DomainCategory.FromPersistence(categoryId, categoryName, DateTime.UtcNow, null, false, null);

        var tip1 = CreateTestTip("Tip 1", "Description 1", categoryId);
        var tip2 = CreateTestTip("Tip 2", "Description 2", categoryId);
        var tips = new List<DomainTip> { tip1, tip2 };

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 20);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _favoritesRepositoryMock
            .Setup(r => r.SearchUserFavoritesAsync(userId, criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync((tips, 2));

        _categoryRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<CategoryId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CategoryId, DomainCategory> { [categoryId] = category });

        var request = new SearchUserFavoritesRequest(userId, criteria);

        // Act
        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Favorites.Should().HaveCount(2);
        result.Value.Favorites[0].TipDetails.Title.Should().Be("Tip 1");
        result.Value.Favorites[0].TipDetails.CategoryName.Should().Be(categoryName);
        result.Value.Favorites[1].TipDetails.Title.Should().Be("Tip 2");
        result.Value.Metadata.TotalItems.Should().Be(2);
        result.Value.Metadata.PageNumber.Should().Be(1);
        result.Value.Metadata.PageSize.Should().Be(20);
        result.Value.Metadata.TotalPages.Should().Be(1);

        _categoryRepositoryMock.Verify(
            r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<CategoryId>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFetchCategoriesInBatch_WhenTipsHaveDifferentCategories()
    {
        // Arrange
        var userId = UserId.NewId();
        var categoryId1 = CategoryId.NewId();
        var categoryId2 = CategoryId.NewId();

        var user = DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("auth123"));

        var category1 = DomainCategory.FromPersistence(categoryId1, "Category 1", DateTime.UtcNow, null, false, null);
        var category2 = DomainCategory.FromPersistence(categoryId2, "Category 2", DateTime.UtcNow, null, false, null);

        var tip1 = CreateTestTip("Tip 1", "Description 1", categoryId1);
        var tip2 = CreateTestTip("Tip 2", "Description 2", categoryId2);
        var tips = new List<DomainTip> { tip1, tip2 };

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 20);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _favoritesRepositoryMock
            .Setup(r => r.SearchUserFavoritesAsync(userId, criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync((tips, 2));

        _categoryRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<CategoryId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CategoryId, DomainCategory>
            {
                [categoryId1] = category1,
                [categoryId2] = category2
            });

        var request = new SearchUserFavoritesRequest(userId, criteria);

        // Act
        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Favorites.Should().HaveCount(2);
        result.Value.Favorites[0].TipDetails.CategoryName.Should().Be("Category 1");
        result.Value.Favorites[1].TipDetails.CategoryName.Should().Be("Category 2");

        // Verify categories were fetched in a single batch call, not N individual calls
        _categoryRepositoryMock.Verify(
            r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<CategoryId>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _categoryRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<CategoryId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = UserId.NewId();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainUser?)null);

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 20);

        var request = new SearchUserFavoritesRequest(userId, criteria);

        // Act
        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyFavorites_WhenUserHasNoFavorites()
    {
        // Arrange
        var userId = UserId.NewId();

        var user = DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("auth123"));

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 20);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _favoritesRepositoryMock
            .Setup(r => r.SearchUserFavoritesAsync(userId, criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DomainTip>(), 0));

        var request = new SearchUserFavoritesRequest(userId, criteria);

        // Act
        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Favorites.Should().BeEmpty();
        result.Value.Metadata.TotalItems.Should().Be(0);

        _categoryRepositoryMock.Verify(
            r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<CategoryId>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseUnknownCategory_WhenCategoryNotFoundInBatch()
    {
        // Arrange
        var userId = UserId.NewId();
        var categoryId = CategoryId.NewId();

        var user = DomainUser.Create(
            Email.Create("test@example.com"),
            UserName.Create("Test User"),
            ExternalAuthIdentifier.Create("auth123"));

        var tip = CreateTestTip("Tip 1", "Description 1", categoryId);
        var tips = new List<DomainTip> { tip };

        var criteria = new TipQueryCriteria(
            SearchTerm: null,
            CategoryId: null,
            Tags: null,
            SortField: TipSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 20);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _favoritesRepositoryMock
            .Setup(r => r.SearchUserFavoritesAsync(userId, criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync((tips, 1));

        _categoryRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<CategoryId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CategoryId, DomainCategory>());

        var request = new SearchUserFavoritesRequest(userId, criteria);

        // Act
        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Favorites.Should().HaveCount(1);
        result.Value.Favorites[0].TipDetails.CategoryName.Should().Be("Unknown Category");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _useCase.ExecuteAsync(null!, CancellationToken.None));

        _userRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static DomainTip CreateTestTip(string title, string description, CategoryId categoryId)
    {
        var steps = new[] { TipStep.Create(1, "This is a test step with enough characters") };
        var tags = new[] { Tag.Create("test") };

        return DomainTip.FromPersistence(
            TipId.NewId(),
            TipTitle.Create(title),
            TipDescription.Create(description),
            steps,
            categoryId,
            tags,
            null,
            DateTime.UtcNow,
            null,
            false,
            null);
    }
}
