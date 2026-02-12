namespace Infrastructure.Configuration;

/// <summary>
/// Configuration options for AWS CloudFront CDN service.
/// </summary>
public class AwsCloudFrontOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "AWS:CloudFront";

    /// <summary>
    /// The CloudFront distribution domain (e.g., "d1234567890abc.cloudfront.net").
    /// </summary>
    public string Domain { get; set; } = string.Empty;
}
