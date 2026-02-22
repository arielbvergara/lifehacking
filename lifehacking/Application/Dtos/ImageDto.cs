namespace Application.Dtos;

public record ImageDto(
    string ImageUrl,
    string ImageStoragePath,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAt
)
{
    /// <summary>
    /// Maps <see cref="Domain.ValueObject.ImageMetadata"/> exception parameter names
    /// to the corresponding DTO field name, prefixed with "Image." for nested field validation.
    /// </summary>
    public static string MapExceptionToFieldName(string? paramName)
    {
        var fieldName = paramName switch
        {
            "imageUrl" => nameof(ImageUrl),
            "imageStoragePath" => nameof(ImageStoragePath),
            "originalFileName" => nameof(OriginalFileName),
            "contentType" => nameof(ContentType),
            "fileSizeBytes" => nameof(FileSizeBytes),
            _ => "Image"
        };

        return paramName is not null && fieldName != "Image"
            ? $"Image.{fieldName}"
            : "Image";
    }
}
