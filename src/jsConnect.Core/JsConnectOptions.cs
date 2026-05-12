using jsConnect.Exceptions;

namespace jsConnect;

/// <summary>
/// Configuration for the jsConnect v3 protocol handler.
/// </summary>
public sealed class JsConnectOptions
{
    /// <summary>
    /// Public client identifier. Issued via the Vanilla dashboard. Sent as the JWT <c>kid</c> header.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Shared HMAC signing secret. Must be at least 16 bytes when UTF-8 encoded.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Signing algorithm for outgoing response JWTs. Default <c>HS256</c>.
    /// </summary>
    public string Algorithm { get; set; } = JsConnectAlgorithms.HS256;

    /// <summary>
    /// Lifetime of the issued response JWT (<c>exp</c> - <c>iat</c>). Default 10 minutes.
    /// </summary>
    public TimeSpan ResponseLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Version string written to the JWT <c>v</c> claim.
    /// </summary>
    public string Version { get; set; } = "aspnetcore:3";

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            throw new InvalidValueException($"{nameof(ClientId)} is required.");
        if (string.IsNullOrEmpty(Secret))
            throw new InvalidValueException($"{nameof(Secret)} is required.");
        if (Algorithm is not (JsConnectAlgorithms.HS256 or JsConnectAlgorithms.HS384 or JsConnectAlgorithms.HS512))
            throw new InvalidValueException($"Unsupported algorithm: {Algorithm}.");

        var minBytes = Algorithm switch
        {
            JsConnectAlgorithms.HS384 => 48,
            JsConnectAlgorithms.HS512 => 64,
            _ => 32, // HS256 — RFC 7518 §3.2 requires a key the same size as the hash output.
        };
        var keyBytes = System.Text.Encoding.UTF8.GetByteCount(Secret);
        if (keyBytes < minBytes)
            throw new InvalidValueException($"For algorithm {Algorithm}, the secret must be at least {minBytes} bytes long (got {keyBytes}).");

        if (ResponseLifetime <= TimeSpan.Zero)
            throw new InvalidValueException($"{nameof(ResponseLifetime)} must be positive.");
    }
}
