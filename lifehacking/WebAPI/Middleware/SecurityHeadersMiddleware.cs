namespace WebAPI.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers to the response, without overwriting any values
        // that may have been set by upstream components (e.g., reverse proxies).
        if (!context.Response.Headers.ContainsKey(SecurityHeaderConstants.XContentTypeOptionsHeaderName))
        {
            context.Response.Headers.Append(
                SecurityHeaderConstants.XContentTypeOptionsHeaderName,
                SecurityHeaderConstants.XContentTypeOptionsNoSniffValue);
        }

        if (!context.Response.Headers.ContainsKey(SecurityHeaderConstants.XFrameOptionsHeaderName))
        {
            context.Response.Headers.Append(
                SecurityHeaderConstants.XFrameOptionsHeaderName,
                SecurityHeaderConstants.XFrameOptionsDenyValue);
        }

        if (!context.Response.Headers.ContainsKey(SecurityHeaderConstants.ReferrerPolicyHeaderName))
        {
            context.Response.Headers.Append(
                SecurityHeaderConstants.ReferrerPolicyHeaderName,
                SecurityHeaderConstants.ReferrerPolicyStrictOriginWhenCrossOriginValue);
        }

        if (!context.Response.Headers.ContainsKey(SecurityHeaderConstants.ContentSecurityPolicyHeaderName))
        {
            context.Response.Headers.Append(
                SecurityHeaderConstants.ContentSecurityPolicyHeaderName,
                SecurityHeaderConstants.ContentSecurityPolicyDefaultSelfValue);
        }

        if (!context.Response.Headers.ContainsKey(SecurityHeaderConstants.PermissionsPolicyHeaderName))
        {
            context.Response.Headers.Append(
                SecurityHeaderConstants.PermissionsPolicyHeaderName,
                SecurityHeaderConstants.PermissionsPolicyRestrictiveValue);
        }

        if (!context.Response.Headers.ContainsKey(SecurityHeaderConstants.XPermittedCrossDomainPoliciesHeaderName))
        {
            context.Response.Headers.Append(
                SecurityHeaderConstants.XPermittedCrossDomainPoliciesHeaderName,
                SecurityHeaderConstants.XPermittedCrossDomainPoliciesNoneValue);
        }

        await next(context);
    }
}
