using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace jsConnect.AspNetCore;

/// <summary>
/// Registration helpers for jsConnect services.
/// </summary>
public static class JsConnectServiceCollectionExtensions
{
    /// <summary>
    /// Register jsConnect services with code-based configuration.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddJsConnect(opts =&gt;
    /// {
    ///     opts.ClientId = "your-client-id";
    ///     opts.Secret   = "your-shared-secret-at-least-16-bytes";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddJsConnect(this IServiceCollection services, Action<JsConnectOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        AddCoreServices(services);
        return services;
    }

    /// <summary>
    /// Register jsConnect services bound to an <see cref="IConfiguration"/> section.
    /// </summary>
    /// <example>
    /// <code>builder.Services.AddJsConnect(builder.Configuration.GetSection("Vanilla"));</code>
    /// </example>
    public static IServiceCollection AddJsConnect(this IServiceCollection services, IConfiguration configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        services.Configure<JsConnectOptions>(configurationSection);
        AddCoreServices(services);
        return services;
    }

    private static void AddCoreServices(IServiceCollection services)
    {
        services.AddOptions<JsConnectOptions>().PostConfigure(o => o.Validate());
        services.AddOptions<ClaimsJsConnectUserResolverOptions>();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<JsConnectOptions>>().Value;
            var clock = sp.GetRequiredService<TimeProvider>();
            return new JsConnectV3(opts, clock);
        });
        services.TryAddSingleton<IJsConnectUserResolver, ClaimsJsConnectUserResolver>();
    }
}
