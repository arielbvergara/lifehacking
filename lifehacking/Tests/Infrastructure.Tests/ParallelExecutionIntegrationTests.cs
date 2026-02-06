using FluentAssertions;
using Infrastructure.Data.Firestore;
using Infrastructure.Repositories;
using Xunit;

namespace Infrastructure.Tests;

/// <summary>
/// Integration tests for parallel test execution with collection namespacing.
/// Validates Requirements 1.3, 9.1, 9.2 from the firestore-test-infrastructure-improvements spec.
/// These tests verify that multiple test instances can run concurrently without data contamination.
/// </summary>
public sealed class ParallelExecutionIntegrationTests
{
    /// <summary>
    /// Verifies that multiple test instances use unique collection names.
    /// This test creates multiple FirestoreTestBase instances and verifies each has a unique collection namespace.
    /// </summary>
    [Fact]
    public void MultipleTestInstances_ShouldUseUniqueCollectionNames_WhenCreatedConcurrently()
    {
        // Arrange: Create multiple test instances (simulating concurrent tests)
        const int instanceCount = 10;
        var testInstances = new List<TestInstance>();

        // Act: Create instances concurrently
        Parallel.For(0, instanceCount, _ =>
        {
            var instance = new TestInstance();
            lock (testInstances)
            {
                testInstances.Add(instance);
            }
        });

        // Assert: All collection names should be unique
        var userCollectionNames = testInstances
            .Select(t => t.CollectionNameProvider.GetCollectionName(FirestoreCollectionNames.Users))
            .ToList();

        var tipCollectionNames = testInstances
            .Select(t => t.CollectionNameProvider.GetCollectionName(FirestoreCollectionNames.Tips))
            .ToList();

        var categoryCollectionNames = testInstances
            .Select(t => t.CollectionNameProvider.GetCollectionName(FirestoreCollectionNames.Categories))
            .ToList();

        // Verify uniqueness
        userCollectionNames.Should().OnlyHaveUniqueItems("each test instance should have a unique users collection");
        tipCollectionNames.Should().OnlyHaveUniqueItems("each test instance should have a unique tips collection");
        categoryCollectionNames.Should().OnlyHaveUniqueItems("each test instance should have a unique categories collection");

        // Verify naming pattern
        userCollectionNames.Should().AllSatisfy(name =>
            name.Should().MatchRegex(@"^users_[a-f0-9]{8}$", "collection name should follow the pattern"));
        tipCollectionNames.Should().AllSatisfy(name =>
            name.Should().MatchRegex(@"^tips_[a-f0-9]{8}$", "collection name should follow the pattern"));
        categoryCollectionNames.Should().AllSatisfy(name =>
            name.Should().MatchRegex(@"^categories_[a-f0-9]{8}$", "collection name should follow the pattern"));

        // Cleanup
        foreach (var instance in testInstances)
        {
            instance.Dispose();
        }
    }

    /// <summary>
    /// Verifies that concurrent test operations don't interfere with each other.
    /// This test runs multiple test scenarios in parallel and verifies data isolation.
    /// </summary>
    [Fact]
    public async Task ConcurrentTests_ShouldNotInterfereWithEachOther_WhenRunningInParallel()
    {
        // Arrange: Create multiple test instances
        const int concurrentTestCount = 5;
        var testTasks = new List<Task<TestExecutionResult>>();

        // Act: Run test scenarios concurrently
        for (int i = 0; i < concurrentTestCount; i++)
        {
            var testNumber = i;
            var task = Task.Run(async () => await ExecuteIsolatedTestScenarioAsync(testNumber));
            testTasks.Add(task);
        }

        // Wait for all tests to complete
        var results = await Task.WhenAll(testTasks);

        // Assert: All tests should succeed without interference
        results.Should().AllSatisfy(result =>
        {
            result.Success.Should().BeTrue($"test {result.TestNumber} should succeed");
            result.ExpectedCategoryCount.Should().Be(result.ActualCategoryCount,
                $"test {result.TestNumber} should find exactly the categories it created");
            result.ExpectedTipCount.Should().Be(result.ActualTipCount,
                $"test {result.TestNumber} should find exactly the tips it created");
            result.ExpectedUserCount.Should().Be(result.ActualUserCount,
                $"test {result.TestNumber} should find exactly the users it created");
        });

        // Verify that each test had different collection names
        var allCollectionNames = results.SelectMany(r => r.CollectionNames).ToList();
        allCollectionNames.Should().OnlyHaveUniqueItems("each test should use unique collection names");
    }

    /// <summary>
    /// Executes an isolated test scenario that creates and queries data.
    /// This simulates a typical test workflow to verify isolation.
    /// </summary>
    private static async Task<TestExecutionResult> ExecuteIsolatedTestScenarioAsync(int testNumber)
    {
        using var testBase = new TestInstance();
        var result = new TestExecutionResult { TestNumber = testNumber };

        try
        {
            // Record collection names for verification
            result.CollectionNames.Add(testBase.CollectionNameProvider.GetCollectionName(FirestoreCollectionNames.Users));
            result.CollectionNames.Add(testBase.CollectionNameProvider.GetCollectionName(FirestoreCollectionNames.Tips));
            result.CollectionNames.Add(testBase.CollectionNameProvider.GetCollectionName(FirestoreCollectionNames.Categories));

            // Create test data specific to this test instance
            const int categoriesPerTest = 3;
            const int tipsPerCategory = 2;
            const int usersPerTest = 2;

            // Create users
            for (int i = 0; i < usersPerTest; i++)
            {
                var user = TestDataFactory.CreateUser(
                    email: $"test{testNumber}_user{i}@example.com",
                    name: $"Test {testNumber} User {i}");
                await testBase.UserRepository.AddAsync(user);
            }

            // Create categories and tips
            for (int i = 0; i < categoriesPerTest; i++)
            {
                var category = TestDataFactory.CreateCategory($"Test {testNumber} Category {i}");
                await testBase.CategoryRepository.AddAsync(category);

                for (int j = 0; j < tipsPerCategory; j++)
                {
                    var tip = TestDataFactory.CreateTip(
                        category.Id,
                        title: $"Test {testNumber} Tip {i}-{j}",
                        description: $"Description for test {testNumber} tip {i}-{j}");
                    await testBase.TipRepository.AddAsync(tip);
                }
            }

            // Small delay to simulate realistic test timing
            await Task.Delay(10);

            // Query data to verify isolation
            var categories = await testBase.CategoryRepository.GetAllAsync();
            var users = await testBase.UserRepository.GetPagedAsync(
                new Application.Dtos.User.UserQueryCriteria(
                    SearchTerm: null,
                    SortField: Application.Dtos.User.UserSortField.CreatedAt,
                    SortDirection: Application.Dtos.SortDirection.Ascending,
                    PageNumber: 1,
                    PageSize: 100,
                    IsDeletedFilter: false
                ));

            // Count tips by querying each category
            var tipCount = 0;
            foreach (var category in categories)
            {
                var tips = await testBase.TipRepository.GetByCategoryAsync(category.Id);
                tipCount += tips.Count;
            }

            // Record results
            result.ExpectedCategoryCount = categoriesPerTest;
            result.ActualCategoryCount = categories.Count;
            result.ExpectedTipCount = categoriesPerTest * tipsPerCategory;
            result.ActualTipCount = tipCount;
            result.ExpectedUserCount = usersPerTest;
            result.ActualUserCount = users.Items.Count;
            result.Success = true;
        }
        catch (Exception)
        {
            result.Success = false;
        }

        return result;
    }

    /// <summary>
    /// Helper class that wraps FirestoreTestBase for testing purposes.
    /// Exposes protected members publicly for test verification.
    /// </summary>
    private sealed class TestInstance : FirestoreTestBase
    {
        // Expose protected members publicly for test access
        public new ICollectionNameProvider CollectionNameProvider => base.CollectionNameProvider;
        public new UserRepository UserRepository => base.UserRepository;
        public new TipRepository TipRepository => base.TipRepository;
        public new CategoryRepository CategoryRepository => base.CategoryRepository;
    }

    /// <summary>
    /// Represents the result of a test execution for verification.
    /// </summary>
    private sealed class TestExecutionResult
    {
        public int TestNumber { get; set; }
        public bool Success { get; set; }
        public int ExpectedCategoryCount { get; set; }
        public int ActualCategoryCount { get; set; }
        public int ExpectedTipCount { get; set; }
        public int ActualTipCount { get; set; }
        public int ExpectedUserCount { get; set; }
        public int ActualUserCount { get; set; }
        public List<string> CollectionNames { get; set; } = new();
    }
}
