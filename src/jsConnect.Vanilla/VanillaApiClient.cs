using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using jsConnect.Vanilla.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace jsConnect.Vanilla;

/// <summary>
/// Read-only client for a small subset of the Vanilla Forums API.
/// </summary>
/// <remarks>
/// This is an <em>opt-in helper</em>, not part of the jsConnect protocol. Use it when you want to
/// look up an existing forum user, e.g. to reconcile a display name. Register via:
/// <code>services.AddHttpClient&lt;VanillaApiClient&gt;(c =&gt; c.BaseAddress = new Uri("https://forums.example.com/"));</code>
/// </remarks>
public sealed class VanillaApiClient
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<VanillaApiClient> _logger;

    public VanillaApiClient(HttpClient httpClient, ILogger<VanillaApiClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        if (httpClient.BaseAddress is null)
            throw new ArgumentException("HttpClient.BaseAddress must be set (e.g. https://forums.example.com/).", nameof(httpClient));

        _httpClient = httpClient;
        _logger = logger ?? NullLogger<VanillaApiClient>.Instance;
    }

    /// <summary>
    /// Look up a Vanilla user by user ID, email, or name. Returns <c>null</c> if not found.
    /// </summary>
    public async Task<VanillaUser?> GetUserAsync(
        int? userId = null,
        string? email = null,
        string? userName = null,
        CancellationToken cancellationToken = default)
    {
        var (param, value) = (userId, email, userName) switch
        {
            ({ } id, _, _) => ("UserId", id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            (_, { Length: > 0 } e, _) => ("Email", e),
            (_, _, { Length: > 0 } n) => ("Name", n),
            _ => throw new ArgumentException("Provide one of userId, email, or userName."),
        };

        var path = $"api/v1/users/get.json?User.{Uri.EscapeDataString(param)}={Uri.EscapeDataString(value)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Vanilla API returned {Status} for {Path}.", (int)response.StatusCode, path);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<VanillaUser>(_serializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Vanilla API call failed.");
            throw;
        }
    }

    /// <summary>
    /// Returns the supplied name, or a numerically suffixed variant if the name is already taken in the forum.
    /// </summary>
    /// <param name="proposedName">The name to start from. May go through <see cref="UsernameSanitizer"/> first.</param>
    /// <param name="maxAttempts">Maximum number of suffixes to try before giving up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<string> GetUniqueUsernameAsync(string proposedName, int maxAttempts = 100, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(proposedName))
            throw new ArgumentException("Proposed name must not be empty.", nameof(proposedName));
        if (maxAttempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidate = attempt == 0 ? proposedName : $"{proposedName}{attempt}";
            var existing = await GetUserAsync(userName: candidate, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (existing is null)
                return candidate;
        }

        throw new InvalidOperationException(
            $"Could not find a unique username for '{proposedName}' after {maxAttempts} attempts.");
    }
}
