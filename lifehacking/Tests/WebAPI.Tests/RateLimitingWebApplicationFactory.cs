namespace WebAPI.Tests;

/// <summary>
/// TEST-ONLY WebApplicationFactory that preserves rate limiting behavior for testing rate limit policies.
/// Inherits all test configuration from CustomWebApplicationFactory but keeps the production rate limiting configuration.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class RateLimitingWebApplicationFactory : CustomWebApplicationFactory
{
    /// <summary>
    /// Preserves rate limiting configuration for rate limiting tests.
    /// </summary>
    protected override bool DisableRateLimiting => false;
}
