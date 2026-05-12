using jsConnect.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace jsConnect.AspNetCore;

/// <summary>
/// Endpoint-routing extensions for the jsConnect v3 authentication endpoint.
/// </summary>
public static class JsConnectEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the jsConnect authentication endpoint at the given route pattern.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">URL pattern, e.g. <c>"/jsconnect"</c>. This is the URL you'll configure in Vanilla's dashboard.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> so callers can chain <c>.RequireAuthorization()</c>, etc.</returns>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// app.MapJsConnect("/jsconnect");
    /// </code>
    /// </example>
    public static IEndpointConventionBuilder MapJsConnect(this IEndpointRouteBuilder endpoints, string pattern)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        return endpoints.MapGet(pattern, (Delegate)HandleAsync);
    }

    private static async Task<IResult> HandleAsync(HttpContext context)
    {
        var jsc = context.RequestServices.GetRequiredService<JsConnectV3>();
        var resolver = context.RequestServices.GetRequiredService<IJsConnectUserResolver>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("jsConnect.AspNetCore");

        var jwt = (string?)context.Request.Query["jwt"];

        try
        {
            var user = await resolver.ResolveAsync(context, context.RequestAborted).ConfigureAwait(false);
            var location = await jsc.GenerateResponseLocationAsync(jwt ?? string.Empty, user, context.RequestAborted).ConfigureAwait(false);
            return Results.Redirect(location, permanent: false, preserveMethod: false);
        }
        catch (JsConnectException ex)
        {
            logger.LogWarning(ex, "jsConnect protocol error: {Message}", ex.Message);
            // Vanilla expects a plain redirect; on error we surface the message as a 400.
            // Vanilla's dashboard will display this when testing the authentication URL.
            return Results.Problem(
                title: "jsConnect protocol error",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                type: ex.GetType().Name);
        }
    }
}
