using System.Text.Json.Serialization;

namespace jsConnect.Vanilla.Models;

/// <summary>A subset of the Vanilla API user response.</summary>
public sealed class VanillaUser
{
    [JsonPropertyName("Profile")]
    public VanillaUserProfile? Profile { get; init; }
}

/// <summary>User profile fields returned by the Vanilla <c>/api/v1/users/get.json</c> endpoint.</summary>
public sealed class VanillaUserProfile
{
    [JsonPropertyName("UserID")] public int? UserID { get; init; }
    [JsonPropertyName("Name")] public string? Name { get; init; }
    [JsonPropertyName("Email")] public string? Email { get; init; }
    [JsonPropertyName("PhotoUrl")] public string? PhotoUrl { get; init; }
    [JsonPropertyName("Photo")] public string? Photo { get; init; }
    [JsonPropertyName("DateInserted")] public string? DateInserted { get; init; }
    [JsonPropertyName("DateLastActive")] public string? DateLastActive { get; init; }
    [JsonPropertyName("CountDiscussions")] public int? CountDiscussions { get; init; }
    [JsonPropertyName("CountComments")] public int? CountComments { get; init; }
    [JsonPropertyName("RankID")] public int? RankID { get; init; }
}
