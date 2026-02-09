using Application.Exceptions;

namespace Application.Validation;

/// <summary>
/// Helper class for aggregating multiple validation failures into a structured
/// <see cref="ValidationException"/> with field-level error detail.
/// </summary>
/// <remarks>
/// This builder allows use cases to collect validation errors from multiple fields
/// before returning a single ValidationException with all errors aggregated.
/// This provides better developer experience by allowing clients to display
/// all validation errors at once rather than one at a time.
/// </remarks>
public class ValidationErrorBuilder
{
    private readonly Dictionary<string, List<string>> _errors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrorBuilder"/> class.
    /// </summary>
    public ValidationErrorBuilder()
    {
        _errors = new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Adds a single validation error for the specified field.
    /// </summary>
    /// <param name="fieldName">The name of the field that failed validation.</param>
    /// <param name="errorMessage">The validation error message.</param>
    /// <exception cref="ArgumentNullException">Thrown when fieldName or errorMessage is null.</exception>
    /// <exception cref="ArgumentException">Thrown when fieldName or errorMessage is empty or whitespace.</exception>
    public void AddError(string fieldName, string errorMessage)
    {
        if (fieldName == null)
        {
            throw new ArgumentNullException(nameof(fieldName));
        }

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Field name cannot be empty or whitespace", nameof(fieldName));
        }

        if (errorMessage == null)
        {
            throw new ArgumentNullException(nameof(errorMessage));
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Error message cannot be empty or whitespace", nameof(errorMessage));
        }

        if (!_errors.ContainsKey(fieldName))
        {
            _errors[fieldName] = new List<string>();
        }

        _errors[fieldName].Add(errorMessage);
    }

    /// <summary>
    /// Adds multiple validation errors for the specified field.
    /// </summary>
    /// <param name="fieldName">The name of the field that failed validation.</param>
    /// <param name="errorMessages">The validation error messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when fieldName or errorMessages is null.</exception>
    /// <exception cref="ArgumentException">Thrown when fieldName is empty or whitespace, or errorMessages is empty.</exception>
    public void AddErrors(string fieldName, params string[] errorMessages)
    {
        if (fieldName == null)
        {
            throw new ArgumentNullException(nameof(fieldName));
        }

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Field name cannot be empty or whitespace", nameof(fieldName));
        }

        if (errorMessages == null)
        {
            throw new ArgumentNullException(nameof(errorMessages));
        }

        if (errorMessages.Length == 0)
        {
            throw new ArgumentException("At least one error message must be provided", nameof(errorMessages));
        }

        foreach (var errorMessage in errorMessages)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error messages cannot be null, empty, or whitespace", nameof(errorMessages));
            }

            AddError(fieldName, errorMessage);
        }
    }

    /// <summary>
    /// Gets a value indicating whether any validation errors have been added.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Builds a <see cref="ValidationException"/> with all aggregated errors.
    /// </summary>
    /// <returns>A ValidationException containing all field-level errors.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no errors have been added.</exception>
    public ValidationException Build()
    {
        return Build("One or more validation errors occurred.");
    }

    /// <summary>
    /// Builds a <see cref="ValidationException"/> with all aggregated errors and a custom message.
    /// </summary>
    /// <param name="overallMessage">The overall error message for the exception.</param>
    /// <returns>A ValidationException containing all field-level errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when overallMessage is null.</exception>
    /// <exception cref="ArgumentException">Thrown when overallMessage is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no errors have been added.</exception>
    public ValidationException Build(string overallMessage)
    {
        if (overallMessage == null)
        {
            throw new ArgumentNullException(nameof(overallMessage));
        }

        if (string.IsNullOrWhiteSpace(overallMessage))
        {
            throw new ArgumentException("Overall message cannot be empty or whitespace", nameof(overallMessage));
        }

        if (!HasErrors)
        {
            throw new InvalidOperationException("Cannot build ValidationException when no errors have been added");
        }

        // Convert List<string> to string[] for each field
        var errorDictionary = _errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray()
        );

        return new ValidationException(overallMessage, errorDictionary);
    }
}
