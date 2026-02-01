using WebAPI.Authentication;

namespace WebAPI.Configuration;

public static class PipelineConfiguration
{
    public static IApplicationBuilder UseAdminUserSeeding(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        if (!hostEnvironment.IsEnvironment("Testing"))
        {
            // Seed the initial admin user if configured. This operation is idempotent and
            // relies on Firebase custom claims for authorization, with a corresponding
            // domain user record for reporting and future domain logic.
            var adminBootstrapper = scope.ServiceProvider.GetRequiredService<IAdminUserBootstrapper>();
            adminBootstrapper.SeedAdminUserAsync().GetAwaiter().GetResult();
        }

        return app;
    }
}
