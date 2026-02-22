namespace Application.Dtos;

public record ImageDto(
    string ImageUrl,
    string ImageStoragePath,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAt
);
