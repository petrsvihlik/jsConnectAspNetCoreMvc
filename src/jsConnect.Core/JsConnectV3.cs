using System.Text;
using System.Text.Json;
using jsConnect.Exceptions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace jsConnect;

/// <summary>
/// Implements the jsConnect v3 protocol.
/// <para>
/// Flow: Vanilla redirects the user to your authentication endpoint with <c>?jwt=&lt;requestJwt&gt;</c>.
/// You validate the JWT, resolve the current user (or guest), and 302-redirect to
/// <c>&lt;rurl&gt;#jwt=&lt;responseJwt&gt;</c> — where <c>rurl</c> is a claim in the request JWT.
/// </para>
/// </summary>
public sealed class JsConnectV3
{
    private const string ClaimRedirectUrl = "rurl";
    private const string ClaimState = "st";
    private const string ClaimUser = "u";
    private const string ClaimVersion = "v";
    private const string QueryFieldJwt = "jwt";

    private static readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };

    private readonly JsConnectOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="options">Signing credentials and protocol settings. <see cref="JsConnectOptions.Validate"/> is invoked.</param>
    /// <param name="timeProvider">Clock source. Defaults to <see cref="TimeProvider.System"/>. Inject a fixed provider in tests.</param>
    public JsConnectV3(JsConnectOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;

        var secretBytes = Encoding.UTF8.GetBytes(options.Secret);
        var signingKey = new SymmetricSecurityKey(secretBytes) { KeyId = options.ClientId };
        _signingCredentials = new SigningCredentials(signingKey, options.Algorithm);

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            RequireExpirationTime = false,
            IssuerSigningKey = signingKey,
            ValidAlgorithms = new[] { JsConnectAlgorithms.HS256, JsConnectAlgorithms.HS384, JsConnectAlgorithms.HS512 },
            ClockSkew = TimeSpan.Zero,
            LifetimeValidator = LifetimeValidator,
        };
    }

    /// <summary>
    /// Build the redirect URL that completes the jsConnect exchange.
    /// </summary>
    /// <param name="requestJwt">The JWT supplied by Vanilla in the <c>?jwt=</c> query parameter.</param>
    /// <param name="user">The currently signed-in user, or <c>null</c> for a guest response.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An absolute URL to 302-redirect the browser to.</returns>
    /// <exception cref="FieldNotFoundException">A required field is missing.</exception>
    /// <exception cref="SignatureInvalidException">The request JWT signature is invalid.</exception>
    /// <exception cref="ExpiredException">The request JWT is expired.</exception>
    /// <exception cref="JsConnectException">Any other protocol failure.</exception>
    public async Task<string> GenerateResponseLocationAsync(string requestJwt, JsConnectUser? user, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(requestJwt))
            throw new FieldNotFoundException(QueryFieldJwt, "query");

        var (rurl, state) = await ValidateAndExtractAsync(requestJwt).ConfigureAwait(false);
        var responseJwt = EncodeResponse(user, state);
        return $"{rurl}#jwt={responseJwt}";
    }

    /// <summary>
    /// Convenience overload that extracts the <c>jwt</c> parameter from a query-string collection.
    /// </summary>
    public Task<string> GenerateResponseLocationAsync(IEnumerable<KeyValuePair<string, string?>> query, JsConnectUser? user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        string? jwt = null;
        foreach (var kv in query)
        {
            if (string.Equals(kv.Key, QueryFieldJwt, StringComparison.Ordinal))
            {
                jwt = kv.Value;
                break;
            }
        }

        if (string.IsNullOrEmpty(jwt))
            throw new FieldNotFoundException(QueryFieldJwt, "query");

        return GenerateResponseLocationAsync(jwt, user, ct);
    }

    private async Task<(string Rurl, JsonElement State)> ValidateAndExtractAsync(string jwt)
    {
        TokenValidationResult result;
        try
        {
            result = await _handler.ValidateTokenAsync(jwt, _validationParameters).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new JsConnectException("Failed to parse the request JWT.", ex);
        }

        if (!result.IsValid)
        {
            throw result.Exception switch
            {
                SecurityTokenExpiredException ex => new ExpiredException(ex.Message, ex),
                SecurityTokenInvalidSignatureException ex => new SignatureInvalidException(ex.Message, ex),
                SecurityTokenInvalidLifetimeException ex => new ExpiredException(ex.Message, ex),
                { } ex => new JsConnectException(ex.Message, ex),
                _ => new JsConnectException("The request JWT is invalid."),
            };
        }

        var token = (JsonWebToken)result.SecurityToken;

        if (!token.TryGetPayloadValue<string>(ClaimRedirectUrl, out var rurl) || string.IsNullOrEmpty(rurl))
            throw new FieldNotFoundException(ClaimRedirectUrl, "request JWT");

        var state = ExtractStateClaim(token);
        return (rurl, state);
    }

    private static JsonElement ExtractStateClaim(JsonWebToken token)
    {
        if (!token.TryGetPayloadValue<JsonElement>(ClaimState, out var element) ||
            element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            using var empty = JsonDocument.Parse("{}");
            return empty.RootElement.Clone();
        }
        return element.Clone();
    }

    private string EncodeResponse(JsConnectUser? user, JsonElement state)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.Add(_options.ResponseLifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            SigningCredentials = _signingCredentials,
            IssuedAt = now,
            Expires = expires,
            Claims = new Dictionary<string, object>
            {
                [ClaimVersion] = _options.Version,
                [ClaimUser] = BuildUserClaim(user),
                [ClaimState] = state,
            },
        };

        return _handler.CreateToken(descriptor);
    }

    private static Dictionary<string, object?> BuildUserClaim(JsConnectUser? user)
    {
        var dict = new Dictionary<string, object?>();
        if (user is null) return dict;

        dict["id"] = user.UniqueId;
        dict["name"] = user.Name;
        dict["email"] = user.Email;
        if (!string.IsNullOrEmpty(user.PhotoUrl))
            dict["photo"] = user.PhotoUrl;
        if (user.Roles is { Count: > 0 })
            dict["roles"] = user.Roles.ToArray();
        if (user.CustomFields is { Count: > 0 })
        {
            foreach (var (key, value) in user.CustomFields)
                dict[key] = value;
        }
        return dict;
    }

    private bool LifetimeValidator(DateTime? notBefore, DateTime? expires, SecurityToken token, TokenValidationParameters parameters)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (notBefore.HasValue && notBefore.Value > now + parameters.ClockSkew)
            return false;
        if (expires.HasValue && expires.Value < now - parameters.ClockSkew)
            return false;
        return true;
    }
}
