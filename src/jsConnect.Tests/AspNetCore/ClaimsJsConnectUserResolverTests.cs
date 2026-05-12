using System.Security.Claims;
using jsConnect.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace jsConnect.Tests.AspNetCore;

public class ClaimsJsConnectUserResolverTests
{
    private static ClaimsJsConnectUserResolver Build(ClaimsJsConnectUserResolverOptions? opts = null) =>
        new(Options.Create(opts ?? new ClaimsJsConnectUserResolverOptions()));

    private static HttpContext WithUser(ClaimsPrincipal user)
    {
        var ctx = new DefaultHttpContext { User = user };
        return ctx;
    }

    private static ClaimsPrincipal AuthenticatedUser(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "TestScheme"));

    [Fact]
    public async Task Returns_null_for_an_unauthenticated_principal()
    {
        var ctx = WithUser(new ClaimsPrincipal(new ClaimsIdentity()));
        var user = await Build().ResolveAsync(ctx, default);
        Assert.Null(user);
    }

    [Fact]
    public async Task Maps_default_claims_to_JsConnectUser_fields()
    {
        var principal = AuthenticatedUser(
            new Claim(ClaimTypes.NameIdentifier, "u-1"),
            new Claim(ClaimTypes.Name, "Alice"),
            new Claim(ClaimTypes.Email, "alice@example.com"),
            new Claim("AvatarUrl", "https://example.com/alice.jpg"),
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "member"));
        var ctx = WithUser(principal);

        var user = await Build().ResolveAsync(ctx, default);

        Assert.NotNull(user);
        Assert.Equal("u-1", user!.UniqueId);
        Assert.Equal("Alice", user.Name);
        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal("https://example.com/alice.jpg", user.PhotoUrl);
        Assert.Equal(new[] { "admin", "member" }, user.Roles);
    }

    [Fact]
    public async Task Returns_null_when_required_claims_are_missing()
    {
        var principal = AuthenticatedUser(new Claim(ClaimTypes.Name, "Alice"));
        var user = await Build().ResolveAsync(WithUser(principal), default);
        Assert.Null(user);
    }

    [Fact]
    public async Task PhotoUrl_and_roles_are_optional()
    {
        var principal = AuthenticatedUser(
            new Claim(ClaimTypes.NameIdentifier, "u-1"),
            new Claim(ClaimTypes.Name, "Alice"),
            new Claim(ClaimTypes.Email, "alice@example.com"));

        var user = await Build().ResolveAsync(WithUser(principal), default);

        Assert.NotNull(user);
        Assert.Null(user!.PhotoUrl);
        Assert.Null(user.Roles);
    }

    [Fact]
    public async Task Honours_custom_claim_type_mapping()
    {
        var customOpts = new ClaimsJsConnectUserResolverOptions
        {
            UniqueIdClaimType = "sub",
            NameClaimType = "preferred_username",
            EmailClaimType = "email",
            PhotoUrlClaimType = "picture",
            RoleClaimType = "roles",
        };
        var principal = AuthenticatedUser(
            new Claim("sub", "12345"),
            new Claim("preferred_username", "bob"),
            new Claim("email", "bob@example.com"),
            new Claim("picture", "https://example.com/bob.jpg"),
            new Claim("roles", "user"));

        var user = await Build(customOpts).ResolveAsync(WithUser(principal), default);

        Assert.NotNull(user);
        Assert.Equal("12345", user!.UniqueId);
        Assert.Equal("bob", user.Name);
        Assert.Equal("bob@example.com", user.Email);
        Assert.Equal("https://example.com/bob.jpg", user.PhotoUrl);
        Assert.Equal(new[] { "user" }, user.Roles);
    }
}
