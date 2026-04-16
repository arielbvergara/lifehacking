using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DbContext");

        if (environment.IsEnvironment("Testing"))
        {
            // In testing, the connection string is replaced by CustomWebApplicationFactory
            // via a DbContext options override. Only register if not already registered.
            if (services.All(d => d.ServiceType != typeof(LifehackingDbContext)))
            {
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    services.AddDbContext<LifehackingDbContext>(opts =>
                        opts.UseNpgsql(connectionString));
                }
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string 'DbContext' is missing from configuration.");
            }

            services.AddDbContext<LifehackingDbContext>(opts =>
                opts.UseNpgsql(connectionString));
        }

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITipRepository, TipRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IFavoritesRepository, FavoritesRepository>();

        return services;
    }
}
