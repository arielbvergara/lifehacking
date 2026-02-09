using Application.Exceptions;
using Application.Validation;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Validation;

public class ValidationErrorBuilderTests
{
    [Fact]
    public void AddError_ShouldAddSingleError_WhenFieldNameAndMessageAreValid()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        builder.AddError("Name", "Name is required");

        // Assert
        builder.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void AddError_ShouldAppendMultipleErrors_WhenCalledMultipleTimesForSameField()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        builder.AddError("Name", "Name is required");
        builder.AddError("Name", "Name must be at least 2 characters");

        // Assert
        builder.HasErrors.Should().BeTrue();
        var exception = builder.Build();
        exception.Errors["Name"].Should().HaveCount(2);
        exception.Errors["Name"].Should().Contain("Name is required");
        exception.Errors["Name"].Should().Contain("Name must be at least 2 characters");
    }

    [Fact]
    public void AddError_ShouldAddErrorsToMultipleFields_WhenCalledForDifferentFields()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        builder.AddError("Name", "Name is required");
        builder.AddError("Email", "Email is invalid");

        // Assert
        builder.HasErrors.Should().BeTrue();
        var exception = builder.Build();
        exception.Errors.Should().HaveCount(2);
        exception.Errors.Should().ContainKey("Name");
        exception.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public void AddError_ShouldThrowArgumentNullException_WhenFieldNameIsNull()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.AddError(null!, "Error message");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fieldName");
    }

    [Fact]
    public void AddError_ShouldThrowArgumentException_WhenFieldNameIsEmpty()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.AddError("", "Error message");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("fieldName")
            .WithMessage("*cannot be empty or whitespace*");
    }

    [Fact]
    public void AddError_ShouldThrowArgumentException_WhenFieldNameIsWhitespace()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.AddError("   ", "Error message");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("fieldName")
            .WithMessage("*cannot be empty or whitespace*");
    }

    [Fact]
    public void AddError_ShouldThrowArgumentNullException_WhenErrorMessageIsNull()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.AddError("Name", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("errorMessage");
    }

    [Fact]
    public void AddError_ShouldThrowArgumentException_WhenErrorMessageIsEmpty()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.AddError("Name", "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("errorMessage")
            .WithMessage("*cannot be empty or whitespace*");
    }

    [Fact]
    public void AddError_ShouldThrowArgumentException_WhenErrorMessageIsWhitespace()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.AddError("Name", "   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("errorMessage")
            .WithMessage("*cannot be empty or whitespace*");
    }

    [Fact]
    public void AddErrors_ShouldAddMultipleErrorsAtOnce_WhenCalledWithMultipleMessages()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        builder.AddErrors("Name", "Name is required", "Name must be at least 2 characters");

        // Assert
        builder.HasErrors.Should().BeTrue();
        var exception = builder.Build();
        exception.Errors["Name"].Should().HaveCount(2);
        exception.Errors["Name"].Should().Contain("Name is required");
        exception.Errors["Name"].Should().Contain("Name must be at least 2 characters");
    }

    [Fact]
    public void AddErrors_ShouldThrowArgumentNullException_WhenFieldNameIsNull()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.AddErrors(null!, "Error 1", "Error 2");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fieldName");
    }

    [Fact]
    public void AddErrors_ShouldThrowArgumentException_WhenFieldNameIsEmpty()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.AddErrors("", "Error 1", "Error 2");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("fieldName")
            .WithMessage("*cannot be empty or whitespace*");
    }

    [Fact]
    public void AddErrors_ShouldThrowArgumentNullException_WhenErrorMessagesIsNull()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.AddErrors("Name", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("errorMessages");
    }

    [Fact]
    public void AddErrors_ShouldThrowArgumentException_WhenErrorMessagesIsEmpty()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.AddErrors("Name");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("errorMessages")
            .WithMessage("*At least one error message must be provided*");
    }

    [Fact]
    public void AddErrors_ShouldThrowArgumentException_WhenAnyErrorMessageIsNull()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.AddErrors("Name", "Valid error", null!, "Another valid error");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("errorMessages")
            .WithMessage("*cannot be null, empty, or whitespace*");
    }

    [Fact]
    public void AddErrors_ShouldThrowArgumentException_WhenAnyErrorMessageIsEmpty()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.AddErrors("Name", "Valid error", "", "Another valid error");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("errorMessages")
            .WithMessage("*cannot be null, empty, or whitespace*");
    }

    [Fact]
    public void HasErrors_ShouldReturnFalse_WhenNoErrorsAdded()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act & Assert
        builder.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void HasErrors_ShouldReturnTrue_WhenErrorsAdded()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        builder.AddError("Name", "Name is required");

        // Assert
        builder.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void Build_ShouldCreateValidationException_WhenErrorsExist()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();
        builder.AddError("Name", "Name is required");
        builder.AddError("Email", "Email is invalid");

        // Act
        var exception = builder.Build();

        // Assert
        exception.Should().BeOfType<ValidationException>();
        exception.Message.Should().Be("One or more validation errors occurred.");
        exception.Errors.Should().HaveCount(2);
        exception.Errors["Name"].Should().ContainSingle().Which.Should().Be("Name is required");
        exception.Errors["Email"].Should().ContainSingle().Which.Should().Be("Email is invalid");
    }

    [Fact]
    public void Build_ShouldThrowInvalidOperationException_WhenNoErrorsAdded()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot build ValidationException when no errors have been added*");
    }

    [Fact]
    public void Build_WithCustomMessage_ShouldCreateValidationExceptionWithCustomMessage_WhenErrorsExist()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();
        builder.AddError("Name", "Name is required");

        // Act
        var exception = builder.Build("Custom validation message");

        // Assert
        exception.Should().BeOfType<ValidationException>();
        exception.Message.Should().Be("Custom validation message");
        exception.Errors.Should().HaveCount(1);
        exception.Errors["Name"].Should().ContainSingle().Which.Should().Be("Name is required");
    }

    [Fact]
    public void Build_WithCustomMessage_ShouldThrowArgumentNullException_WhenMessageIsNull()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();
        builder.AddError("Name", "Name is required");

        // Act
        var act = () => builder.Build(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("overallMessage");
    }

    [Fact]
    public void Build_WithCustomMessage_ShouldThrowArgumentException_WhenMessageIsEmpty()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();
        builder.AddError("Name", "Name is required");

        // Act
        var act = () => builder.Build("");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("overallMessage")
            .WithMessage("*cannot be empty or whitespace*");
    }

    [Fact]
    public void Build_WithCustomMessage_ShouldThrowInvalidOperationException_WhenNoErrorsAdded()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();

        // Act
        var act = () => builder.Build("Custom message");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot build ValidationException when no errors have been added*");
    }

    [Fact]
    public void Build_ShouldConvertErrorListsToArrays_WhenMultipleErrorsPerField()
    {
        // Arrange
        var builder = new ValidationErrorBuilder();
        builder.AddError("Name", "Error 1");
        builder.AddError("Name", "Error 2");
        builder.AddError("Name", "Error 3");

        // Act
        var exception = builder.Build();

        // Assert
        exception.Errors["Name"].Should().BeOfType<string[]>();
        exception.Errors["Name"].Should().HaveCount(3);
        exception.Errors["Name"][0].Should().Be("Error 1");
        exception.Errors["Name"][1].Should().Be("Error 2");
        exception.Errors["Name"][2].Should().Be("Error 3");
    }
}
