namespace jsConnect;

/// <summary>
/// User payload returned from the jsConnect authentication endpoint.
/// Field names map to the v3 protocol: <c>id</c>, <c>name</c>, <c>email</c>, <c>photo</c>, <c>roles</c>.
/// </summary>
public sealed class JsConnectUser
{
    /// <summary>The unique identifier of the user in your application. Required.</summary>
    public required string UniqueId { get; init; }

    /// <summary>The user's display name. Required.</summary>
    public required string Name { get; init; }

    /// <summary>The user's email address. Required.</summary>
    public required string Email { get; init; }

    /// <summary>Optional avatar URL.</summary>
    public string? PhotoUrl { get; init; }

    /// <summary>Optional roles. Strings or integer IDs (Vanilla accepts both).</summary>
    public IReadOnlyCollection<string>? Roles { get; init; }

    /// <summary>Additional custom fields to include in the response JWT's <c>u</c> claim.</summary>
    public IReadOnlyDictionary<string, object?>? CustomFields { get; init; }
}
