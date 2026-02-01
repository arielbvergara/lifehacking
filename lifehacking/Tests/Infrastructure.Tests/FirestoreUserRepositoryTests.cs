using Application.Dtos.User;
using Domain.Entities;
using Domain.ValueObject;
using FluentAssertions;
using Google.Cloud.Firestore;
using Infrastructure.Data.Firestore;
using Infrastructure.Repositories;
using Xunit;

namespace Infrastructure.Tests;

public class FirestoreUserRepositoryTests
{
    private const string EmulatorHostEnvironmentVariableName = "FIRESTORE_EMULATOR_HOST";
    private const string DefaultTestProjectId = "lifehacking-test";

    private static bool TryCreateRepository(out FirestoreUserRepository repository)
    {
        var emulatorHost = Environment.GetEnvironmentVariable(EmulatorHostEnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(emulatorHost))
        {
            repository = null!;
            return false;
        }

        // Ensure the Firestore client points at the emulator. The emulator host value
        // itself is provided externally (e.g., via test runner configuration) so that
        // test code does not need to know specific host/port details.
        Environment.SetEnvironmentVariable(EmulatorHostEnvironmentVariableName, emulatorHost);

        var firestoreDb = FirestoreDb.Create(DefaultTestProjectId);
        var firestoreUserDataStore = new FirestoreUserDataStore(firestoreDb);
        repository = new FirestoreUserRepository(firestoreUserDataStore);
        return true;
    }

    [Fact]
    public async Task AddAsync_ShouldPersistAndRetrieveUser_WhenUsingFirestoreEmulator()
    {
        if (!TryCreateRepository(out var repository))
        {
            // When the emulator is not configured, treat this test as a no-op so that
            // local development and CI can opt in by setting FIRESTORE_EMULATOR_HOST.
            return;
        }

        var email = Email.Create($"user-{Guid.NewGuid():N}@example.com");
        var name = UserName.Create("Firestore Test User");
        var externalAuthId = ExternalAuthIdentifier.Create($"external-{Guid.NewGuid():N}");

        var user = User.Create(email, name, externalAuthId);

        await repository.AddAsync(user, CancellationToken.None);

        var reloaded = await repository.GetByIdAsync(user.Id, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(user.Id);
        reloaded.Email.Value.Should().Be(email.Value);
        reloaded.Name.Value.Should().Be(name.Value);
        reloaded.ExternalAuthId.Value.Should().Be(externalAuthId.Value);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnFilteredAndPagedResults_WhenUsingCriteria()
    {
        if (!TryCreateRepository(out var repository))
        {
            return;
        }

        // Use a unique prefix so this test remains stable even if the emulator
        // contains documents from previous runs.
        var emailPrefix = $"firestore-paging-{Guid.NewGuid():N}";

        for (var index = 0; index < 15; index++)
        {
            var email = Email.Create($"{emailPrefix}-{index}@example.com");
            var name = UserName.Create($"User {index}");
            var externalAuthId = ExternalAuthIdentifier.Create($"external-{emailPrefix}-{index}");

            var user = User.Create(email, name, externalAuthId);
            await repository.AddAsync(user, CancellationToken.None);
        }

        var criteria = new UserQueryCriteria(
            emailPrefix,
            UserSortField.CreatedAt,
            SortDirection.Descending,
            PageNumber: 2,
            PageSize: 5,
            IsDeletedFilter: null);

        var (items, totalCount) = await repository.GetPagedAsync(criteria, CancellationToken.None);

        totalCount.Should().BeGreaterOrEqualTo(10);
        items.Should().HaveCount(5);
        items.Should().OnlyContain(user => user.Email.Value.Contains(emailPrefix, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteUser_WhenUsingFirestoreEmulator()
    {
        if (!TryCreateRepository(out var repository))
        {
            return;
        }

        var email = Email.Create($"softdelete-{Guid.NewGuid():N}@example.com");
        var name = UserName.Create("Soft Delete User");
        var externalAuthId = ExternalAuthIdentifier.Create($"external-softdelete-{Guid.NewGuid():N}");
        var user = User.Create(email, name, externalAuthId);

        await repository.AddAsync(user, CancellationToken.None);

        // Act: soft delete the user via the repository.
        await repository.DeleteAsync(user.Id, CancellationToken.None);

        // Assert: default GetByIdAsync should behave as if the user no longer exists.
        var fromRepo = await repository.GetByIdAsync(user.Id, CancellationToken.None);
        fromRepo.Should().BeNull();

        // But querying with IsDeletedFilter = true should surface the soft-deleted user.
        var criteria = new UserQueryCriteria(
            SearchTerm: null,
            SortField: UserSortField.CreatedAt,
            SortDirection: SortDirection.Descending,
            PageNumber: 1,
            PageSize: 10,
            IsDeletedFilter: true);

        var (deletedItems, deletedTotalCount) = await repository.GetPagedAsync(criteria, CancellationToken.None);

        deletedTotalCount.Should().BeGreaterThan(0);
        deletedItems.Should().Contain(u => u.Id == user.Id && u.IsDeleted);
    }
}
