using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace jsConnect.Tests.AspNetCore;

/// <summary>
/// Authenticates every request as the same hard-coded user (for endpoint integration tests).
/// </summary>
internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<TestAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthenticationOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Options.PretendAnonymous)
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(Options.Claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal sealed class TestAuthenticationOptions : AuthenticationSchemeOptions
{
    public IList<Claim> Claims { get; set; } = [];
    public bool PretendAnonymous { get; set; }
}
