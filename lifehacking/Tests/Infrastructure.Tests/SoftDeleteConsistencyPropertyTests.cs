using System.Reflection;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Infrastructure.Tests;

/// <summary>
/// Structural consistency tests for soft delete pattern across all entities.
/// Feature: firestore-test-infrastructure-improvements
///
/// WHY THIS TEST CLASS EXISTS:
/// This spec adds soft delete support to Tip and Category entities, which User already had.
/// To ensure consistency across the domain model, all three entities (User, Tip, Category)
/// must follow the same soft delete pattern. This test class uses REFLECTION to verify
/// structural consistency:
/// - All entities have IsDeleted (bool) property
/// - All entities have DeletedAt (DateTime?) property
/// - All entities have MarkDeleted() method
/// - All FromPersistence methods accept isDeleted and deletedAt parameters
///
/// These are STRUCTURAL tests (not behavioral). They verify the API surface is consistent,
/// making the domain model predictable and maintainable. If a developer adds a new entity
/// or modifies the soft delete pattern, these tests will catch inconsistencies.
///
/// RELATIONSHIP TO OTHER TESTS:
/// - TipSoftDeletePropertyTests / CategorySoftDeletePropertyTests: Test entity BEHAVIOR
/// - This class: Tests entity STRUCTURE for consistency
///
/// Tests Property 11: Soft Delete Consistency Across Entities
/// </summary>
public sealed class SoftDeleteConsistencyPropertyTests
{
    // Feature: firestore-test-infrastructure-improvements, Property 11: Soft Delete Consistency Across Entities
    // For all entity types (User, Tip, Category), the soft delete pattern should be structurally consistent:
    // each entity should have IsDeleted (bool), DeletedAt (DateTime?), and MarkDeleted() method, and the
    // FromPersistence factory methods should accept IsDeleted and DeletedAt parameters.
    // Validates: Requirements 10.1, 10.3

    /// <summary>
    /// Property: All entities (User, Tip, Category) should have IsDeleted property of type bool.
    /// This property verifies structural consistency of the IsDeleted property.
    /// </summary>
    [Fact]
    public void AllEntities_ShouldHaveIsDeletedProperty_WithBoolType()
    {
        // Arrange: Get entity types
        var entityTypes = new[] { typeof(User), typeof(Tip), typeof(Category) };

        // Act & Assert: Check each entity type
        foreach (var entityType in entityTypes)
        {
            var isDeletedProperty = entityType.GetProperty("IsDeleted", BindingFlags.Public | BindingFlags.Instance);

            isDeletedProperty.Should().NotBeNull($"{entityType.Name} should have an IsDeleted property");
            isDeletedProperty!.PropertyType.Should().Be(typeof(bool),
                $"{entityType.Name}.IsDeleted should be of type bool");
            isDeletedProperty.CanRead.Should().BeTrue(
                $"{entityType.Name}.IsDeleted should be readable");
        }
    }

    /// <summary>
    /// Property: All entities (User, Tip, Category) should have DeletedAt property of type DateTime?.
    /// This property verifies structural consistency of the DeletedAt property.
    /// </summary>
    [Fact]
    public void AllEntities_ShouldHaveDeletedAtProperty_WithNullableDateTimeType()
    {
        // Arrange: Get entity types
        var entityTypes = new[] { typeof(User), typeof(Tip), typeof(Category) };

        // Act & Assert: Check each entity type
        foreach (var entityType in entityTypes)
        {
            var deletedAtProperty = entityType.GetProperty("DeletedAt", BindingFlags.Public | BindingFlags.Instance);

            deletedAtProperty.Should().NotBeNull($"{entityType.Name} should have a DeletedAt property");
            deletedAtProperty!.PropertyType.Should().Be(typeof(DateTime?),
                $"{entityType.Name}.DeletedAt should be of type DateTime?");
            deletedAtProperty.CanRead.Should().BeTrue(
                $"{entityType.Name}.DeletedAt should be readable");
        }
    }

    /// <summary>
    /// Property: All entities (User, Tip, Category) should have a MarkDeleted() method.
    /// This property verifies structural consistency of the MarkDeleted method.
    /// </summary>
    [Fact]
    public void AllEntities_ShouldHaveMarkDeletedMethod_WithNoParameters()
    {
        // Arrange: Get entity types
        var entityTypes = new[] { typeof(User), typeof(Tip), typeof(Category) };

        // Act & Assert: Check each entity type
        foreach (var entityType in entityTypes)
        {
            var markDeletedMethod = entityType.GetMethod("MarkDeleted",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            markDeletedMethod.Should().NotBeNull($"{entityType.Name} should have a MarkDeleted() method");
            markDeletedMethod!.ReturnType.Should().Be(typeof(void),
                $"{entityType.Name}.MarkDeleted() should return void");
            markDeletedMethod.GetParameters().Should().BeEmpty(
                $"{entityType.Name}.MarkDeleted() should have no parameters");
        }
    }

    /// <summary>
    /// Property: User.FromPersistence should accept isDeleted and deletedAt parameters.
    /// This property verifies the FromPersistence method signature for User.
    /// </summary>
    [Fact]
    public void UserFromPersistence_ShouldAcceptSoftDeleteParameters_InSignature()
    {
        // Arrange: Get User type
        var userType = typeof(User);

        // Act: Find FromPersistence method
        var fromPersistenceMethod = userType.GetMethod("FromPersistence",
            BindingFlags.Public | BindingFlags.Static);

        // Assert: Method should exist
        fromPersistenceMethod.Should().NotBeNull("User should have a FromPersistence static method");

        // Assert: Method should have isDeleted parameter
        var parameters = fromPersistenceMethod!.GetParameters();
        var isDeletedParam = parameters.FirstOrDefault(p => p.Name == "isDeleted");
        isDeletedParam.Should().NotBeNull("User.FromPersistence should have an isDeleted parameter");
        isDeletedParam!.ParameterType.Should().Be(typeof(bool),
            "User.FromPersistence isDeleted parameter should be of type bool");

        // Assert: Method should have deletedAt parameter
        var deletedAtParam = parameters.FirstOrDefault(p => p.Name == "deletedAt");
        deletedAtParam.Should().NotBeNull("User.FromPersistence should have a deletedAt parameter");
        deletedAtParam!.ParameterType.Should().Be(typeof(DateTime?),
            "User.FromPersistence deletedAt parameter should be of type DateTime?");
    }

    /// <summary>
    /// Property: Tip.FromPersistence should accept isDeleted and deletedAt parameters.
    /// This property verifies the FromPersistence method signature for Tip.
    /// </summary>
    [Fact]
    public void TipFromPersistence_ShouldAcceptSoftDeleteParameters_InSignature()
    {
        // Arrange: Get Tip type
        var tipType = typeof(Tip);

        // Act: Find FromPersistence method
        var fromPersistenceMethod = tipType.GetMethod("FromPersistence",
            BindingFlags.Public | BindingFlags.Static);

        // Assert: Method should exist
        fromPersistenceMethod.Should().NotBeNull("Tip should have a FromPersistence static method");

        // Assert: Method should have isDeleted parameter
        var parameters = fromPersistenceMethod!.GetParameters();
        var isDeletedParam = parameters.FirstOrDefault(p => p.Name == "isDeleted");
        isDeletedParam.Should().NotBeNull("Tip.FromPersistence should have an isDeleted parameter");
        isDeletedParam!.ParameterType.Should().Be(typeof(bool),
            "Tip.FromPersistence isDeleted parameter should be of type bool");

        // Assert: Method should have deletedAt parameter
        var deletedAtParam = parameters.FirstOrDefault(p => p.Name == "deletedAt");
        deletedAtParam.Should().NotBeNull("Tip.FromPersistence should have a deletedAt parameter");
        deletedAtParam!.ParameterType.Should().Be(typeof(DateTime?),
            "Tip.FromPersistence deletedAt parameter should be of type DateTime?");
    }

    /// <summary>
    /// Property: Category.FromPersistence should accept isDeleted and deletedAt parameters.
    /// This property verifies the FromPersistence method signature for Category.
    /// </summary>
    [Fact]
    public void CategoryFromPersistence_ShouldAcceptSoftDeleteParameters_InSignature()
    {
        // Arrange: Get Category type
        var categoryType = typeof(Category);

        // Act: Find FromPersistence method
        var fromPersistenceMethod = categoryType.GetMethod("FromPersistence",
            BindingFlags.Public | BindingFlags.Static);

        // Assert: Method should exist
        fromPersistenceMethod.Should().NotBeNull("Category should have a FromPersistence static method");

        // Assert: Method should have isDeleted parameter
        var parameters = fromPersistenceMethod!.GetParameters();
        var isDeletedParam = parameters.FirstOrDefault(p => p.Name == "isDeleted");
        isDeletedParam.Should().NotBeNull("Category.FromPersistence should have an isDeleted parameter");
        isDeletedParam!.ParameterType.Should().Be(typeof(bool),
            "Category.FromPersistence isDeleted parameter should be of type bool");

        // Assert: Method should have deletedAt parameter
        var deletedAtParam = parameters.FirstOrDefault(p => p.Name == "deletedAt");
        deletedAtParam.Should().NotBeNull("Category.FromPersistence should have a deletedAt parameter");
        deletedAtParam!.ParameterType.Should().Be(typeof(DateTime?),
            "Category.FromPersistence deletedAt parameter should be of type DateTime?");
    }
}
