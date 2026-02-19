using Application.Dtos.User;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.User;

public class DeleteUserUseCase(
    IUserRepository userRepository,
    IUserOwnershipService userOwnershipService,
    IFavoritesRepository favoritesRepository,
    IIdentityProviderService identityProviderService)
{
    public async Task<Result<bool, AppException>> ExecuteAsync(DeleteUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserId.Create(request.UserId);

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result<bool, AppException>.Fail(new NotFoundException("User", request.UserId));
            }

            var ownershipError = await userOwnershipService.EnsureOwnerOrAdminAsync(
                user,
                request.CurrentUser,
                request.UserId,
                cancellationToken);

            if (ownershipError is not null)
            {
                return Result<bool, AppException>.Fail(ownershipError);
            }

            // Delete user's favorites to maintain referential integrity
            await favoritesRepository.RemoveAllByUserAsync(userId, cancellationToken);

            // Soft delete the user in our database
            await userRepository.DeleteAsync(userId, cancellationToken);

            // Delete the user from Firebase Authentication
            await identityProviderService.DeleteUserAsync(user.ExternalAuthId.Value, cancellationToken);

            return Result<bool, AppException>.Ok(true);
        }
        catch (AppException ex)
        {
            return Result<bool, AppException>.Fail(ex);
        }
        catch (ArgumentException ex)
        {
            return Result<bool, AppException>.Fail(new ValidationException(ex.Message));
        }
        catch (Exception ex)
        {
            return Result<bool, AppException>.Fail(new InfraException("An unexpected error occurred", ex));
        }
    }
}
