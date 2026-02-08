using Application.Interfaces;
using Application.Services;
using Application.UseCases.Category;
using Application.UseCases.Favorite;
using Application.UseCases.Tip;
using Application.UseCases.User;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        // User services
        services.AddScoped<IUserOwnershipService, UserOwnershipService>();

        // User use cases
        services.AddScoped<CreateUserUseCase>();
        services.AddScoped<CreateAdminUserUseCase>();
        services.AddScoped<GetUserByIdUseCase>();
        services.AddScoped<GetUserByEmailUseCase>();
        services.AddScoped<UpdateUserNameUseCase>();
        services.AddScoped<DeleteUserUseCase>();
        services.AddScoped<GetUserByExternalAuthIdUseCase>();
        services.AddScoped<GetUsersUseCase>();

        // Category use cases
        services.AddScoped<GetCategoriesUseCase>();
        services.AddScoped<GetTipsByCategoryUseCase>();
        services.AddScoped<CreateCategoryUseCase>();
        services.AddScoped<UpdateCategoryUseCase>();
        services.AddScoped<DeleteCategoryUseCase>();

        // Tip use cases
        services.AddScoped<GetTipByIdUseCase>();
        services.AddScoped<SearchTipsUseCase>();
        services.AddScoped<CreateTipUseCase>();
        services.AddScoped<UpdateTipUseCase>();
        services.AddScoped<DeleteTipUseCase>();

        // Favorite use cases
        services.AddScoped<SearchUserFavoritesUseCase>();
        services.AddScoped<AddFavoriteUseCase>();
        services.AddScoped<RemoveFavoriteUseCase>();
        services.AddScoped<MergeFavoritesUseCase>();

        return services;
    }
}
