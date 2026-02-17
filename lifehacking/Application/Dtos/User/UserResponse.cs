namespace Application.Dtos.User;

public record UserResponse(
    Guid Id,
    string Email,
    string Name,
    string ExternalAuthId,
    string Role,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsDeleted
);
