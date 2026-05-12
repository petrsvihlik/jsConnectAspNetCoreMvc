using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace jsConnect.AspNetCore;

/// <summary>
/// Default <see cref="IJsConnectUserResolver"/> that reads user data from <see cref="HttpContext.User"/> claims.
/// </summary>
/// <remarks>
/// Claim mapping (configurable via <see cref="ClaimsJsConnectUserResolverOptions"/>):
/// <list type="bullet">
///   <item><see cref="ClaimTypes.NameIdentifier"/> → <c>id</c></item>
///   <item><see cref="ClaimTypes.Name"/> → <c>name</c></item>
///   <item><see cref="ClaimTypes.Email"/> → <c>email</c></item>
///   <item><c>AvatarUrl</c> → <c>photo</c></item>
///   <item><see cref="ClaimTypes.Role"/> (all instances) → <c>roles</c></item>
/// </list>
/// </remarks>
public sealed class ClaimsJsConnectUserResolver : IJsConnectUserResolver
{
    private readonly ClaimsJsConnectUserResolverOptions _options;

    public ClaimsJsConnectUserResolver(IOptions<ClaimsJsConnectUserResolverOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public Task<JsConnectUser?> ResolveAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var principal = context.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return Task.FromResult<JsConnectUser?>(null);

        var id = principal.FindFirst(_options.UniqueIdClaimType)?.Value;
        var name = principal.FindFirst(_options.NameClaimType)?.Value;
        var email = principal.FindFirst(_options.EmailClaimType)?.Value;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
            return Task.FromResult<JsConnectUser?>(null);

        var photo = principal.FindFirst(_options.PhotoUrlClaimType)?.Value;
        var roles = principal.FindAll(_options.RoleClaimType)
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToArray();

        var user = new JsConnectUser
        {
            UniqueId = id,
            Name = name,
            Email = email,
            PhotoUrl = string.IsNullOrEmpty(photo) ? null : photo,
            Roles = roles.Length == 0 ? null : roles,
        };

        return Task.FromResult<JsConnectUser?>(user);
    }
}

/// <summary>Claim-type mapping for <see cref="ClaimsJsConnectUserResolver"/>.</summary>
public sealed class ClaimsJsConnectUserResolverOptions
{
    public string UniqueIdClaimType { get; set; } = ClaimTypes.NameIdentifier;
    public string NameClaimType { get; set; } = ClaimTypes.Name;
    public string EmailClaimType { get; set; } = ClaimTypes.Email;
    public string PhotoUrlClaimType { get; set; } = "AvatarUrl";
    public string RoleClaimType { get; set; } = ClaimTypes.Role;
}
