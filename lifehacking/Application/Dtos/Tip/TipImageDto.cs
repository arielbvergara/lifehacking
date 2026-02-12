namespace Application.Dtos.Tip;

public record TipImageDto(
    string ImageUrl,
    string ImageStoragePath,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAt
);
