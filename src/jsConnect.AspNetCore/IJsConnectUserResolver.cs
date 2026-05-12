using Microsoft.AspNetCore.Http;

namespace jsConnect.AspNetCore;

/// <summary>
/// Resolves the currently authenticated user from an <see cref="HttpContext"/> into a <see cref="JsConnectUser"/>.
/// Return <c>null</c> to signal a guest (anonymous) response.
/// </summary>
public interface IJsConnectUserResolver
{
    /// <summary>
    /// Resolve the current user, or <c>null</c> if no user is signed in.
    /// </summary>
    Task<JsConnectUser?> ResolveAsync(HttpContext context, CancellationToken cancellationToken);
}
