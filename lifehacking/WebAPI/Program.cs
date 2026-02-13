using Application;
using Application.Interfaces;
using Infrastructure.Configuration;
using Infrastructure.Logging;
using WebAPI.Authentication;
using WebAPI.Configuration;
using WebAPI.Filters;
using WebAPI.Middleware;

namespace WebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        // Support `~` in GOOGLE_APPLICATION_CREDENTIALS path
        SetGoogleApplicationCredentialsPath();

        var builder = WebApplication.CreateBuilder(args);

        // Observability and monitoring configuration (Sentry, etc.)
        var isSentryEnabled = builder.ConfigureSentry();

        // Add services to the container.
        builder.Services.AddControllers(options => { options.Filters.Add<GlobalExceptionFilter>(); });

        // Swagger/OpenAPI configuration
        builder.Services.AddSwaggerConfiguration();

        // CORS configuration
        builder.Services.AddCorsConfiguration(builder.Configuration, builder.Environment);

        // Database configuration
        builder.Services.AddDatabaseConfiguration(builder.Configuration, builder.Environment);

        // AWS configuration (S3, CloudFront)
        builder.Services.AddAwsConfiguration(builder.Configuration);

        // Rate Limiting configuration
        builder.Services.AddRateLimitingConfiguration();

        // Authentication & Authorization
        builder.Services.AddJwtAuthenticationAndAuthorization(builder.Configuration, builder.Environment);

        // Application use cases
        builder.Services.AddUseCases();

        // Security event notifier
        builder.Services.AddScoped<ISecurityEventNotifier, LoggingSecurityEventNotifier>();

        // Application-level observability service (logs + optional Sentry)
        builder.Services.AddScoped<IObservabilityService, SentryObservabilityService>();

        // Admin user seeding configuration and services
        builder.Services.Configure<AdminUserOptions>(
            builder.Configuration.GetSection(AdminUserOptions.SectionName)
        );

        // Firebase database configuration (used by infrastructure persistence components)
        builder.Services.Configure<FirebaseDatabaseOptions>(
            builder.Configuration.GetSection(FirebaseDatabaseOptions.SectionName)
        );

        builder.Services.AddSingleton<IFirebaseAdminClient, FirebaseAdminClient>();
        builder.Services.AddSingleton<IIdentityProviderService>(sp => sp.GetRequiredService<IFirebaseAdminClient>());
        builder.Services.AddScoped<IAdminUserBootstrapper, AdminUserBootstrapper>();

        var app = builder.Build();

        // Swagger
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();

        // Correlation ID must be established early so it flows through logging
        // scopes and error handling.
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Security Headers
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // CORS must run before authentication/authorization so that preflight
        // and actual requests receive the proper headers.
        app.UseCors(CorsConfiguration.GetCorsPolicyName());

        app.UseRateLimiter();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Sentry performance tracing (when enabled via configuration)
        if (isSentryEnabled)
        {
            app.UseSentryTracing();
        }

        app.MapControllers();

        // Admin user seeding (Firebase-based persistence only; no EF Core migrations)
        app.UseAdminUserSeeding();

        // Lightweight health endpoint for integration and uptime checks.
        app.MapGet("/health", () => Results.Ok())
            .AllowAnonymous();

        app.Run();
    }

    private static void SetGoogleApplicationCredentialsPath()
    {
        // If GOOGLE_APPLICATION_CREDENTIALS_JSON is provided, write it to a temp file
        // and set GOOGLE_APPLICATION_CREDENTIALS to point to that file.
        // This allows passing credentials as an environment variable in cloud deployments.
        var credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");
        if (!string.IsNullOrEmpty(credentialsJson))
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"firebase-credentials-{Guid.NewGuid()}.json");
            File.WriteAllText(tempPath, credentialsJson);
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", tempPath);
            return;
        }

        // Support `~` in GOOGLE_APPLICATION_CREDENTIALS path for local development
        var googleCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (string.IsNullOrEmpty(googleCredentials) || !googleCredentials.StartsWith('~'))
        {
            return;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var expandedPath = googleCredentials.Replace("~", home);
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", expandedPath);
    }
}
