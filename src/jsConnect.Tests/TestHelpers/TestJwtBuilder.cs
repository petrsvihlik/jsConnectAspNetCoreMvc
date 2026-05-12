using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace jsConnect.Tests.TestHelpers;

/// <summary>
/// Builds the JWTs Vanilla would send to the authentication endpoint, so tests can drive
/// <see cref="JsConnectV3"/> end-to-end without a real forum.
/// </summary>
internal static class TestJwtBuilder
{
    private static readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };

    // 64 bytes — satisfies the RFC 7518 minimum for HS256/HS384/HS512.
    public const string DefaultSecret = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    public const string DefaultClientId = "test-client-id";
    public const string DefaultRedirectUrl = "https://forums.example.com/sso/callback";

    public static string CreateRequestJwt(
        string secret = DefaultSecret,
        string clientId = DefaultClientId,
        string redirectUrl = DefaultRedirectUrl,
        string algorithm = JsConnectAlgorithms.HS256,
        IDictionary<string, object?>? state = null,
        TimeSpan? lifetime = null,
        DateTimeOffset? issuedAt = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)) { KeyId = clientId };
        var creds = new SigningCredentials(key, algorithm);
        var now = (issuedAt ?? DateTimeOffset.UtcNow).UtcDateTime;
        var expires = now.Add(lifetime ?? TimeSpan.FromMinutes(10));

        var claims = new Dictionary<string, object>
        {
            ["rurl"] = redirectUrl,
            ["v"] = "test:3",
        };

        if (state is not null)
            claims["st"] = ToJsonElement(state);

        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            SigningCredentials = creds,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Claims = claims,
        });
    }

    /// <summary>Decode and verify a JWT produced by <see cref="JsConnectV3"/>.</summary>
    public static async Task<JsonWebToken> DecodeResponseAsync(string jwt, string secret = DefaultSecret)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidAlgorithms = new[] { "HS256", "HS384", "HS512" },
        };
        var result = await _handler.ValidateTokenAsync(jwt, validationParameters);
        if (!result.IsValid)
            throw new InvalidOperationException("Test response JWT failed validation: " + result.Exception?.Message);
        return (JsonWebToken)result.SecurityToken;
    }

    /// <summary>Pull <c>jwt=...</c> out of a redirect URL fragment.</summary>
    public static string ExtractResponseJwt(string redirectUrl)
    {
        var hash = redirectUrl.IndexOf('#');
        if (hash < 0) throw new InvalidOperationException("Redirect URL has no fragment.");
        var fragment = redirectUrl[(hash + 1)..];
        var prefix = "jwt=";
        if (!fragment.StartsWith(prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Fragment is not of the form 'jwt=...'.");
        return fragment[prefix.Length..];
    }

    private static JsonElement ToJsonElement(object value)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(value);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
