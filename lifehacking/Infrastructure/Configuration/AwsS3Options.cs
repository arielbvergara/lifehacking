namespace Infrastructure.Configuration;

/// <summary>
/// Configuration options for AWS S3 storage service.
/// </summary>
public class AwsS3Options
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "AWS:S3";

    /// <summary>
    /// The name of the S3 bucket for storing images.
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// The AWS region where the S3 bucket is located (e.g., "eu-central-1").
    /// </summary>
    public string Region { get; set; } = "eu-central-1";
}
