using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace NorthflankWeather.AppHost;

/// <summary>
/// Configuration for an authentication provider.
/// </summary>
public sealed record AuthProviderConfig
{
    public required string ProviderName { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string Audience { get; init; }
    public required bool RequireHttpsMetadata { get; init; }
    public required string NameClaimType { get; init; }
    public required string RoleClaimType { get; init; }

    /// <summary>
    /// Static authority URL for external providers (e.g., Duende Demo).
    /// </summary>
    public string? Authority { get; init; }

    /// <summary>
    /// Container resource for local providers (e.g., FusionAuth).
    /// When set, the Authority will be derived from this container's endpoint.
    /// </summary>
    public IResourceBuilder<ContainerResource>? AuthResource { get; init; }

    /// <summary>
    /// Whether to revoke refresh tokens on logout.
    /// Some providers (like FusionAuth) don't expose a revocation endpoint in OIDC discovery.
    /// Default is true for providers that support it.
    /// </summary>
    public bool RevokeRefreshTokenOnLogout { get; init; } = true;

    /// <summary>
    /// Path suffix to append to the container endpoint for the authority URL.
    /// Used by providers like Keycloak that include the realm in the authority path.
    /// Example: "/realms/aspire" for Keycloak.
    /// </summary>
    public string? AuthorityPathSuffix { get; init; }

    /// <summary>
    /// Returns true if this is a container-based provider (like FusionAuth).
    /// </summary>
    public bool IsContainerBased => AuthResource is not null;
}

/// <summary>
/// Extension methods for configuring resources with auth provider settings.
/// </summary>
public static class AuthProviderExtensions
{
    /// <summary>
    /// Applies server authentication environment variables to a resource.
    /// </summary>
    public static IResourceBuilder<T> WithServerAuth<T>(
        this IResourceBuilder<T> resource,
        AuthProviderConfig authProvider) where T : IResourceWithEnvironment
    {
        resource
            .WithEnvironment("Auth__ClientId", authProvider.ClientId)
            .WithEnvironment("Auth__ClientSecret", authProvider.ClientSecret)
            .WithEnvironment("Auth__Audience", authProvider.Audience)
            .WithEnvironment("Auth__RequireHttpsMetadata", authProvider.RequireHttpsMetadata.ToString())
            .WithEnvironment("Auth__NameClaimType", authProvider.NameClaimType)
            .WithEnvironment("Auth__RoleClaimType", authProvider.RoleClaimType);

        // Apply authority - either static URL or container endpoint
        if (authProvider.Authority is not null)
        {
            resource.WithEnvironment("Auth__Authority", authProvider.Authority);
        }
        else if (authProvider.AuthResource is not null)
        {
            if (authProvider.AuthorityPathSuffix is not null)
            {
                // Build authority URL with path suffix (e.g., for Keycloak realms)
                resource.WithEnvironment(context =>
                {
                    var endpoint = authProvider.AuthResource.GetEndpoint("http");
                    context.EnvironmentVariables["Auth__Authority"] = ReferenceExpression.Create(
                        $"{endpoint}{authProvider.AuthorityPathSuffix}");
                });
            }
            else
            {
                resource.WithEnvironment("Auth__Authority", authProvider.AuthResource.GetEndpoint("http"));
            }
        }

        return resource;
    }

    /// <summary>
    /// Applies BFF authentication environment variables to a resource.
    /// </summary>
    public static IResourceBuilder<T> WithBffAuth<T>(
        this IResourceBuilder<T> resource,
        AuthProviderConfig authProvider) where T : IResourceWithEnvironment
    {
        resource
            .WithEnvironment("Auth__ClientId", authProvider.ClientId)
            .WithEnvironment("Auth__ClientSecret", authProvider.ClientSecret)
            .WithEnvironment("Auth__Audience", authProvider.Audience)
            .WithEnvironment("Auth__RequireHttpsMetadata", authProvider.RequireHttpsMetadata.ToString())
            .WithEnvironment("Auth__NameClaimType", authProvider.NameClaimType)
            .WithEnvironment("Auth__RoleClaimType", authProvider.RoleClaimType)
            .WithEnvironment("Auth__RevokeRefreshTokenOnLogout", authProvider.RevokeRefreshTokenOnLogout.ToString());

        // Apply authority - either static URL or container endpoint
        if (authProvider.Authority is not null)
        {
            resource.WithEnvironment("Auth__Authority", authProvider.Authority);
        }
        else if (authProvider.AuthResource is not null)
        {
            if (authProvider.AuthorityPathSuffix is not null)
            {
                // Build authority URL with path suffix (e.g., for Keycloak realms)
                resource.WithEnvironment(context =>
                {
                    var endpoint = authProvider.AuthResource.GetEndpoint("http");
                    context.EnvironmentVariables["Auth__Authority"] = ReferenceExpression.Create(
                        $"{endpoint}{authProvider.AuthorityPathSuffix}");
                });
            }
            else
            {
                resource.WithEnvironment("Auth__Authority", authProvider.AuthResource.GetEndpoint("http"));
            }
        }

        return resource;
    }
}

/// <summary>
/// Factory methods for creating authentication provider configurations.
/// </summary>
public static class AuthProviders
{
    /// <summary>
    /// Creates a configuration for the Duende Demo identity server.
    /// This is a public demo server useful for testing.
    /// </summary>
    public static AuthProviderConfig DuendeDemo() => new()
    {
        ProviderName = "DuendeDemo",
        Authority = "https://demo.duendesoftware.com",
        ClientId = "interactive.confidential",
        ClientSecret = "secret",
        Audience = "api",
        RequireHttpsMetadata = true,
        NameClaimType = "name",
        RoleClaimType = "role",
        AuthResource = null
    };


}
