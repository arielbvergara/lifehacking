using Application.Dtos;
using Application.Dtos.User;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Xunit;

namespace Infrastructure.Tests;

[Trait("Category", "Integration")]
public sealed class UserRepositoryTests : FirestoreTestBase
{
    public UserRepositoryTests()
    {
        // Clean up any existing test data before each test
        CleanupTestDataAsync().Wait();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistAndRetrieveUser_WhenUsingFirestoreEmulator()
    {
        var email = Email.Create($"user-{Guid.NewGuid():N}@example.com");
        var name = UserName.Create("Firestore Test User");
        var externalAuthId = ExternalAuthIdentifier.Create($"external-{Guid.NewGuid():N}");

        var user = User.Create(email, name, externalAuthId);

        await UserRepository.AddAsync(user, CancellationToken.None);

        var reloaded = await UserRepository.GetByIdAsync(user.Id, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(user.Id);
        reloaded.Email.Value.Should().Be(email.Value);
        reloaded.Name.Value.Should().Be(name.Value);
        reloaded.ExternalAuthId.Value.Should().Be(externalAuthId.Value);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnFilteredAndPagedResults_WhenUsingCriteria()
    {
        // Use a unique prefix so this test remains stable even if the emulator
        // contains documents from previous runs.
        var emailPrefix = $"firestore-paging-{Guid.NewGuid():N}";

        for (var index = 0; index < 15; index++)
        {
            var email = Email.Create($"{emailPrefix}-{index}@example.com");
            var name = UserName.Create($"User {index}");
            var externalAuthId = ExternalAuthIdentifier.Create($"external-{emailPrefix}-{index}");

            var user = User.Create(email, name, externalAuthId);
            await UserRepository.AddAsync(user, CancellationToken.None);
        }

        var criteria = new UserQueryCriteria(
            emailPrefix,
            UserSortField.CreatedAt,
            SortDirection.Descending,
            PageNumber: 2,
            PageSize: 5,
            IsDeletedFilter: null);

        var (items, totalCount) = await UserRepository.GetPagedAsync(criteria, CancellationToken.None);

        totalCount.Should().BeGreaterOrEqualTo(10);
        items.Should().HaveCount(5);
        items.Should().OnlyContain(user => user.Email.Value.Contains(emailPrefix, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteUser_WhenUsingFirestoreEmulator()
    {
        var email = Email.Create($"softdelete-{Guid.NewGuid():N}@example.com");
        var name = UserName.Create("Soft Delete User");
        var externalAuthId = ExternalAuthIdentifier.Create($"external-softdelete-{Guid.NewGuid():N}");
        var user = User.Create(email, name, externalAuthId);

        await UserRepository.AddAsync(user, CancellationToken.None);

        // Act: soft delete the user via the repository.
        await UserRepository.DeleteAsync(user.Id, CancellationToken.None);

        // Assert: default GetByIdAsync should behave as if the user no longer exists.
        var fromRepo = await UserRepository.GetByIdAsync(user.Id, CancellationToken.None);
        fromRepo.Should().BeNull();

        // But querying with IsDeletedFilter = true should surface the soft-deleted user.
        var criteria = new UserQueryCriteria(
            SearchTerm: null,
            SortField: UserSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 10,
            IsDeletedFilter: true);

        var (deletedItems, deletedTotalCount) = await UserRepository.GetPagedAsync(criteria, CancellationToken.None);

        deletedTotalCount.Should().BeGreaterThan(0);
        deletedItems.Should().Contain(u => u.Id == user.Id && u.IsDeleted);
    }
}
