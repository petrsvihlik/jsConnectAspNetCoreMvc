using System.Net;
using System.Text;
using System.Text.Json;
using jsConnect.Vanilla;
using jsConnect.Vanilla.Models;
using Xunit;

namespace jsConnect.Tests.Vanilla;

public class VanillaApiClientTests
{
    private static readonly Uri BaseAddress = new("https://forums.example.com/");

    private static VanillaApiClient Build(StubHandler handler, Uri? baseAddress = null) =>
        new(new HttpClient(handler) { BaseAddress = baseAddress ?? BaseAddress });

    [Fact]
    public void Constructor_requires_a_base_address_on_the_HttpClient()
    {
        var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));
        Assert.Throws<ArgumentException>(() => new VanillaApiClient(client));
    }

    [Fact]
    public async Task GetUserAsync_returns_null_on_404()
    {
        var client = Build(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));
        var result = await client.GetUserAsync(email: "missing@example.com");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserAsync_returns_user_on_200_with_payload()
    {
        var payload = new VanillaUser
        {
            Profile = new VanillaUserProfile { UserID = 7, Name = "alice", Email = "alice@example.com" },
        };
        var client = Build(new StubHandler(_ => Json(payload)));

        var result = await client.GetUserAsync(email: "alice@example.com");

        Assert.NotNull(result);
        Assert.Equal(7, result!.Profile!.UserID);
        Assert.Equal("alice", result.Profile.Name);
    }

    [Fact]
    public async Task GetUserAsync_builds_the_correct_query_for_each_lookup_strategy()
    {
        string? capturedPath = null;
        var handler = new StubHandler(req =>
        {
            capturedPath = req.RequestUri!.PathAndQuery;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var client = Build(handler);

        await client.GetUserAsync(userId: 42);
        Assert.Equal("/api/v1/users/get.json?User.UserId=42", capturedPath);

        await client.GetUserAsync(email: "alice@example.com");
        Assert.Equal("/api/v1/users/get.json?User.Email=alice%40example.com", capturedPath);

        await client.GetUserAsync(userName: "alice smith");
        Assert.Equal("/api/v1/users/get.json?User.Name=alice%20smith", capturedPath);
    }

    [Fact]
    public async Task GetUserAsync_throws_when_no_identifier_is_provided()
    {
        var client = Build(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetUserAsync());
    }

    [Fact]
    public async Task GetUserAsync_returns_null_on_unexpected_5xx_without_throwing()
    {
        var client = Build(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var result = await client.GetUserAsync(email: "a@b.com");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUniqueUsernameAsync_returns_the_original_when_unused()
    {
        var client = Build(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));
        var name = await client.GetUniqueUsernameAsync("alice");
        Assert.Equal("alice", name);
    }

    [Fact]
    public async Task GetUniqueUsernameAsync_appends_a_numeric_suffix_when_taken()
    {
        var hits = 0;
        var client = Build(new StubHandler(req =>
        {
            // First two lookups (alice, alice1) return existing users; third lookup returns 404.
            hits++;
            if (hits <= 2)
                return Json(new VanillaUser { Profile = new VanillaUserProfile { Name = "x" } });
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var name = await client.GetUniqueUsernameAsync("alice");

        Assert.Equal("alice2", name);
    }

    [Fact]
    public async Task GetUniqueUsernameAsync_gives_up_after_max_attempts()
    {
        var client = Build(new StubHandler(_ =>
            Json(new VanillaUser { Profile = new VanillaUserProfile { Name = "x" } })));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetUniqueUsernameAsync("alice", maxAttempts: 3));
    }

    [Fact]
    public async Task GetUniqueUsernameAsync_rejects_empty_input()
    {
        var client = Build(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetUniqueUsernameAsync(""));
    }

    private static HttpResponseMessage Json<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
