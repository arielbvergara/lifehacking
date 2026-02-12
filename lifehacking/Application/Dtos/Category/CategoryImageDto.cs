namespace Application.Dtos.Category;

public record CategoryImageDto(
    string ImageUrl,
    string ImageStoragePath,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAt
);
