using System.Net;
using System.Security.Claims;
using System.Text.Json;
using jsConnect.AspNetCore;
using jsConnect.Tests.TestHelpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace jsConnect.Tests.AspNetCore;

public class JsConnectEndpointTests
{
    private static async Task<IHost> StartHostAsync(
        Action<TestAuthenticationOptions>? configureAuth = null,
        Action<IServiceCollection>? extraServices = null,
        string route = "/jsconnect")
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services
                        .AddAuthentication(TestAuthenticationHandler.SchemeName)
                        .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(
                            TestAuthenticationHandler.SchemeName, configureAuth ?? (_ => { }));
                    services.AddJsConnect(opts =>
                    {
                        opts.ClientId = TestJwtBuilder.DefaultClientId;
                        opts.Secret = TestJwtBuilder.DefaultSecret;
                    });
                    extraServices?.Invoke(services);
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseEndpoints(endpoints => endpoints.MapJsConnect(route));
                });
            });

        var host = await builder.StartAsync();
        return host;
    }

    [Fact]
    public async Task Authenticated_request_returns_302_with_response_jwt_in_fragment()
    {
        using var host = await StartHostAsync(opts =>
        {
            opts.Claims =
            [
                new Claim(ClaimTypes.NameIdentifier, "u-1"),
                new Claim(ClaimTypes.Name, "Alice"),
                new Claim(ClaimTypes.Email, "alice@example.com"),
            ];
        });
        var client = host.GetTestClient();
        client.DefaultRequestVersion = HttpVersion.Version11;

        var reqJwt = TestJwtBuilder.CreateRequestJwt();
        var response = await client.GetAsync($"/jsconnect?jwt={Uri.EscapeDataString(reqJwt)}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith(TestJwtBuilder.DefaultRedirectUrl + "#jwt=", location);

        var token = await TestJwtBuilder.DecodeResponseAsync(TestJwtBuilder.ExtractResponseJwt(location));
        var u = token.GetPayloadValue<JsonElement>("u");
        Assert.Equal("u-1", u.GetProperty("id").GetString());
        Assert.Equal("Alice", u.GetProperty("name").GetString());
        Assert.Equal("alice@example.com", u.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Anonymous_request_yields_a_guest_response()
    {
        using var host = await StartHostAsync(opts => opts.PretendAnonymous = true);
        var client = host.GetTestClient();

        var reqJwt = TestJwtBuilder.CreateRequestJwt();
        var response = await client.GetAsync($"/jsconnect?jwt={Uri.EscapeDataString(reqJwt)}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var token = await TestJwtBuilder.DecodeResponseAsync(
            TestJwtBuilder.ExtractResponseJwt(response.Headers.Location!.ToString()));

        var u = token.GetPayloadValue<JsonElement>("u");
        Assert.Empty(u.EnumerateObject());
    }

    [Fact]
    public async Task Missing_jwt_query_parameter_returns_400()
    {
        using var host = await StartHostAsync(opts => opts.PretendAnonymous = true);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/jsconnect");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Tampered_jwt_returns_400()
    {
        using var host = await StartHostAsync(opts => opts.PretendAnonymous = true);
        var client = host.GetTestClient();

        var reqJwt = TestJwtBuilder.CreateRequestJwt(secret: "DIFFERENT-secret-of-32-chars-min");
        var response = await client.GetAsync($"/jsconnect?jwt={Uri.EscapeDataString(reqJwt)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Custom_resolver_replaces_the_default()
    {
        using var host = await StartHostAsync(
            opts => opts.PretendAnonymous = true,
            extra => extra.AddSingleton<IJsConnectUserResolver, StaticUserResolver>());
        var client = host.GetTestClient();

        var reqJwt = TestJwtBuilder.CreateRequestJwt();
        var response = await client.GetAsync($"/jsconnect?jwt={Uri.EscapeDataString(reqJwt)}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var token = await TestJwtBuilder.DecodeResponseAsync(
            TestJwtBuilder.ExtractResponseJwt(response.Headers.Location!.ToString()));
        var u = token.GetPayloadValue<JsonElement>("u");
        Assert.Equal("from-custom-resolver", u.GetProperty("id").GetString());
    }

    [Fact]
    public async Task Custom_route_pattern_is_honoured()
    {
        using var host = await StartHostAsync(
            opts => opts.PretendAnonymous = true,
            route: "/sso/vanilla");
        var client = host.GetTestClient();

        var reqJwt = TestJwtBuilder.CreateRequestJwt();
        var response = await client.GetAsync($"/sso/vanilla?jwt={Uri.EscapeDataString(reqJwt)}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private sealed class StaticUserResolver : IJsConnectUserResolver
    {
        public Task<JsConnectUser?> ResolveAsync(HttpContext context, CancellationToken cancellationToken)
            => Task.FromResult<JsConnectUser?>(new JsConnectUser
            {
                UniqueId = "from-custom-resolver",
                Name = "Custom",
                Email = "custom@example.com",
            });
    }
}
