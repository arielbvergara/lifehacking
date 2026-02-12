using Amazon.S3;
using Application.Interfaces;
using Infrastructure.Configuration;
using Infrastructure.Storage;

namespace WebAPI.Configuration;

/// <summary>
/// Extension methods for configuring AWS services in the application.
/// </summary>
public static class AwsConfiguration
{
    /// <summary>
    /// Registers AWS S3 and CloudFront services with dependency injection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAwsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind AWS S3 options from configuration
        services.Configure<AwsS3Options>(
            configuration.GetSection(AwsS3Options.SectionName)
        );

        // Bind AWS CloudFront options from configuration
        services.Configure<AwsCloudFrontOptions>(
            configuration.GetSection(AwsCloudFrontOptions.SectionName)
        );

        // Register AWS S3 client
        // The SDK will automatically use credentials from environment variables,
        // IAM roles, or AWS credentials file
        services.AddAWSService<IAmazonS3>();

        // Register image storage service implementation
        services.AddScoped<IImageStorageService, S3ImageStorageService>();

        return services;
    }
}
