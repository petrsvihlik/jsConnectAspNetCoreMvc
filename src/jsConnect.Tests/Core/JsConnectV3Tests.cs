using System.Text.Json;
using jsConnect.Exceptions;
using jsConnect.Tests.TestHelpers;
using Xunit;

namespace jsConnect.Tests.Core;

public class JsConnectV3Tests
{
    private static JsConnectOptions DefaultOptions(string algo = JsConnectAlgorithms.HS256) => new()
    {
        ClientId = TestJwtBuilder.DefaultClientId,
        Secret = TestJwtBuilder.DefaultSecret,
        Algorithm = algo,
    };

    private static readonly JsConnectUser SampleUser = new()
    {
        UniqueId = "user-123",
        Name = "alice",
        Email = "alice@example.com",
        PhotoUrl = "https://example.com/alice.jpg",
        Roles = new[] { "admin", "member" },
    };

    [Fact]
    public void Constructor_throws_on_null_options()
    {
        Assert.Throws<ArgumentNullException>(() => new JsConnectV3(null!));
    }

    [Fact]
    public void Constructor_runs_options_validation()
    {
        var opts = new JsConnectOptions { ClientId = "c", Secret = "too-short" };
        Assert.Throws<InvalidValueException>(() => new JsConnectV3(opts));
    }

    [Fact]
    public async Task GenerateResponseLocationAsync_returns_redirect_with_jwt_fragment()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        var reqJwt = TestJwtBuilder.CreateRequestJwt();

        var location = await jsc.GenerateResponseLocationAsync(reqJwt, SampleUser);

        Assert.StartsWith(TestJwtBuilder.DefaultRedirectUrl + "#jwt=", location);
        var responseJwt = TestJwtBuilder.ExtractResponseJwt(location);
        Assert.False(string.IsNullOrEmpty(responseJwt));
    }

    [Fact]
    public async Task Response_user_claim_uses_v3_field_names_and_includes_optional_fields()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        var reqJwt = TestJwtBuilder.CreateRequestJwt();

        var location = await jsc.GenerateResponseLocationAsync(reqJwt, SampleUser);
        var token = await TestJwtBuilder.DecodeResponseAsync(TestJwtBuilder.ExtractResponseJwt(location));

        Assert.True(token.TryGetPayloadValue<JsonElement>("u", out var u));
        Assert.Equal("user-123", u.GetProperty("id").GetString());
        Assert.Equal("alice", u.GetProperty("name").GetString());
        Assert.Equal("alice@example.com", u.GetProperty("email").GetString());
        Assert.Equal("https://example.com/alice.jpg", u.GetProperty("photo").GetString());
        var roles = u.GetProperty("roles").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(new[] { "admin", "member" }, roles);
    }

    [Fact]
    public async Task Response_omits_optional_fields_when_user_does_not_supply_them()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        var reqJwt = TestJwtBuilder.CreateRequestJwt();

        var minimal = new JsConnectUser { UniqueId = "1", Name = "n", Email = "e@e.com" };
        var location = await jsc.GenerateResponseLocationAsync(reqJwt, minimal);
        var token = await TestJwtBuilder.DecodeResponseAsync(TestJwtBuilder.ExtractResponseJwt(location));

        var u = token.GetPayloadValue<JsonElement>("u");
        Assert.False(u.TryGetProperty("photo", out _));
        Assert.False(u.TryGetProperty("roles", out _));
    }

    [Fact]
    public async Task Guest_response_emits_an_empty_user_object()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        var reqJwt = TestJwtBuilder.CreateRequestJwt();

        var location = await jsc.GenerateResponseLocationAsync(reqJwt, user: null);
        var token = await TestJwtBuilder.DecodeResponseAsync(TestJwtBuilder.ExtractResponseJwt(location));

        var u = token.GetPayloadValue<JsonElement>("u");
        Assert.Equal(JsonValueKind.Object, u.ValueKind);
        Assert.Empty(u.EnumerateObject());
    }

    [Fact]
    public async Task Custom_fields_are_passed_through_to_the_user_claim()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        var reqJwt = TestJwtBuilder.CreateRequestJwt();

        var user = new JsConnectUser
        {
            UniqueId = "1",
            Name = "n",
            Email = "e@e.com",
            CustomFields = new Dictionary<string, object?>
            {
                ["department"] = "eng",
                ["seats"] = 4,
            },
        };
        var location = await jsc.GenerateResponseLocationAsync(reqJwt, user);
        var token = await TestJwtBuilder.DecodeResponseAsync(TestJwtBuilder.ExtractResponseJwt(location));

        var u = token.GetPayloadValue<JsonElement>("u");
        Assert.Equal("eng", u.GetProperty("department").GetString());
        Assert.Equal(4, u.GetProperty("seats").GetInt32());
    }

    [Fact]
    public async Task State_claim_is_echoed_back_unchanged()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        var state = new Dictionary<string, object?>
        {
            ["sourceUrl"] = "https://forums.example.com/discussions",
            ["nested"] = new Dictionary<string, object?> { ["key"] = "value" },
        };
        var reqJwt = TestJwtBuilder.CreateRequestJwt(state: state);

        var location = await jsc.GenerateResponseLocationAsync(reqJwt, SampleUser);
        var token = await TestJwtBuilder.DecodeResponseAsync(TestJwtBuilder.ExtractResponseJwt(location));

        var st = token.GetPayloadValue<JsonElement>("st");
        Assert.Equal("https://forums.example.com/discussions", st.GetProperty("sourceUrl").GetString());
        Assert.Equal("value", st.GetProperty("nested").GetProperty("key").GetString());
    }

    [Fact]
    public async Task Missing_state_claim_in_request_yields_empty_object_in_response()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        var reqJwt = TestJwtBuilder.CreateRequestJwt(state: null);

        var location = await jsc.GenerateResponseLocationAsync(reqJwt, SampleUser);
        var token = await TestJwtBuilder.DecodeResponseAsync(TestJwtBuilder.ExtractResponseJwt(location));

        var st = token.GetPayloadValue<JsonElement>("st");
        Assert.Equal(JsonValueKind.Object, st.ValueKind);
        Assert.Empty(st.EnumerateObject());
    }

    [Fact]
    public async Task Response_includes_iat_exp_v_and_kid()
    {
        var fixed_ = new FixedTimeProvider(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
        var opts = DefaultOptions();
        opts.ResponseLifetime = TimeSpan.FromMinutes(7);
        var jsc = new JsConnectV3(opts, fixed_);
        var reqJwt = TestJwtBuilder.CreateRequestJwt(issuedAt: fixed_.GetUtcNow());

        var location = await jsc.GenerateResponseLocationAsync(reqJwt, SampleUser);
        var token = await TestJwtBuilder.DecodeResponseAsync(TestJwtBuilder.ExtractResponseJwt(location));

        Assert.Equal(TestJwtBuilder.DefaultClientId, token.Kid);
        Assert.Equal("aspnetcore:3", token.GetPayloadValue<string>("v"));

        var iat = token.GetPayloadValue<long>("iat");
        var exp = token.GetPayloadValue<long>("exp");
        Assert.Equal(fixed_.GetUtcNow().ToUnixTimeSeconds(), iat);
        Assert.Equal(fixed_.GetUtcNow().Add(TimeSpan.FromMinutes(7)).ToUnixTimeSeconds(), exp);
    }

    [Theory]
    [InlineData(JsConnectAlgorithms.HS256)]
    [InlineData(JsConnectAlgorithms.HS384)]
    [InlineData(JsConnectAlgorithms.HS512)]
    public async Task All_three_HMAC_algorithms_round_trip(string algo)
    {
        var jsc = new JsConnectV3(DefaultOptions(algo));
        var reqJwt = TestJwtBuilder.CreateRequestJwt(algorithm: algo);

        var location = await jsc.GenerateResponseLocationAsync(reqJwt, SampleUser);
        var token = await TestJwtBuilder.DecodeResponseAsync(TestJwtBuilder.ExtractResponseJwt(location));

        Assert.Equal(algo, token.Alg);
    }

    [Fact]
    public async Task A_request_signed_with_a_different_algorithm_is_still_accepted_when_supported()
    {
        // Vanilla may sign with any HS* algo; we should accept all three regardless of our configured signing algo.
        var jsc = new JsConnectV3(DefaultOptions(JsConnectAlgorithms.HS256));
        var reqJwt = TestJwtBuilder.CreateRequestJwt(algorithm: JsConnectAlgorithms.HS512);

        var location = await jsc.GenerateResponseLocationAsync(reqJwt, SampleUser);
        Assert.Contains("#jwt=", location);
    }

    [Fact]
    public async Task A_request_with_an_invalid_signature_is_rejected()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        var reqJwt = TestJwtBuilder.CreateRequestJwt(secret: "another-secret-of-32-chars-min!!");

        await Assert.ThrowsAsync<SignatureInvalidException>(
            () => jsc.GenerateResponseLocationAsync(reqJwt, SampleUser));
    }

    [Fact]
    public async Task An_expired_request_JWT_is_rejected()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        var reqJwt = TestJwtBuilder.CreateRequestJwt(
            issuedAt: DateTimeOffset.UtcNow.AddHours(-1),
            lifetime: TimeSpan.FromMinutes(1));

        await Assert.ThrowsAsync<ExpiredException>(
            () => jsc.GenerateResponseLocationAsync(reqJwt, SampleUser));
    }

    [Fact]
    public async Task A_garbage_jwt_string_is_rejected_as_a_JsConnectException()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        await Assert.ThrowsAsync<JsConnectException>(
            () => jsc.GenerateResponseLocationAsync("not-a-jwt", SampleUser));
    }

    [Fact]
    public async Task Empty_jwt_input_throws_FieldNotFoundException()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        var ex = await Assert.ThrowsAsync<FieldNotFoundException>(
            () => jsc.GenerateResponseLocationAsync("", SampleUser));
        Assert.Equal("jwt", ex.FieldName);
    }

    [Fact]
    public async Task Query_overload_finds_jwt_parameter()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        var reqJwt = TestJwtBuilder.CreateRequestJwt();
        var query = new[]
        {
            new KeyValuePair<string, string?>("other", "x"),
            new KeyValuePair<string, string?>("jwt", reqJwt),
        };

        var location = await jsc.GenerateResponseLocationAsync(query, SampleUser);
        Assert.Contains("#jwt=", location);
    }

    [Fact]
    public async Task Query_overload_throws_when_jwt_is_missing()
    {
        var jsc = new JsConnectV3(DefaultOptions());
        var query = new[] { new KeyValuePair<string, string?>("other", "x") };

        await Assert.ThrowsAsync<FieldNotFoundException>(
            () => jsc.GenerateResponseLocationAsync(query, SampleUser));
    }

    [Fact]
    public async Task A_request_jwt_with_no_rurl_claim_throws_FieldNotFoundException()
    {
        // Build a JWT manually that lacks the rurl claim.
        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(TestJwtBuilder.DefaultSecret));
        var descriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key, JsConnectAlgorithms.HS256),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { ["v"] = "test:3" },
        };
        var reqJwt = handler.CreateToken(descriptor);

        var jsc = new JsConnectV3(DefaultOptions());
        var ex = await Assert.ThrowsAsync<FieldNotFoundException>(
            () => jsc.GenerateResponseLocationAsync(reqJwt, SampleUser));
        Assert.Equal("rurl", ex.FieldName);
    }
}
