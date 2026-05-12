using jsConnect.Exceptions;
using jsConnect.Tests.TestHelpers;
using Xunit;

namespace jsConnect.Tests.Core;

public class JsConnectOptionsTests
{
    [Fact]
    public void Validate_passes_for_a_valid_options_instance()
    {
        var opts = new JsConnectOptions
        {
            ClientId = TestJwtBuilder.DefaultClientId,
            Secret = TestJwtBuilder.DefaultSecret,
        };
        opts.Validate(); // does not throw
    }

    [Fact]
    public void Validate_requires_a_client_id()
    {
        var opts = new JsConnectOptions { Secret = TestJwtBuilder.DefaultSecret };
        Assert.Throws<InvalidValueException>(opts.Validate);
    }

    [Fact]
    public void Validate_requires_a_secret()
    {
        var opts = new JsConnectOptions { ClientId = "c" };
        Assert.Throws<InvalidValueException>(opts.Validate);
    }

    [Theory]
    [InlineData(JsConnectAlgorithms.HS256, 31)] // one byte under
    [InlineData(JsConnectAlgorithms.HS384, 47)]
    [InlineData(JsConnectAlgorithms.HS512, 63)]
    public void Validate_rejects_secrets_shorter_than_the_algorithms_minimum(string algo, int bytes)
    {
        var opts = new JsConnectOptions
        {
            ClientId = "c",
            Secret = new string('a', bytes),
            Algorithm = algo,
        };
        Assert.Throws<InvalidValueException>(opts.Validate);
    }

    [Theory]
    [InlineData(JsConnectAlgorithms.HS256, 32)]
    [InlineData(JsConnectAlgorithms.HS384, 48)]
    [InlineData(JsConnectAlgorithms.HS512, 64)]
    public void Validate_accepts_secrets_at_the_algorithms_minimum(string algo, int bytes)
    {
        var opts = new JsConnectOptions
        {
            ClientId = "c",
            Secret = new string('a', bytes),
            Algorithm = algo,
        };
        opts.Validate();
    }

    [Theory]
    [InlineData("HS256")]
    [InlineData("HS384")]
    [InlineData("HS512")]
    public void Validate_accepts_supported_algorithms(string algo)
    {
        var opts = new JsConnectOptions
        {
            ClientId = "c",
            Secret = TestJwtBuilder.DefaultSecret,
            Algorithm = algo,
        };
        opts.Validate();
    }

    [Theory]
    [InlineData("RS256")]
    [InlineData("none")]
    [InlineData("")]
    public void Validate_rejects_unsupported_algorithms(string algo)
    {
        var opts = new JsConnectOptions
        {
            ClientId = "c",
            Secret = TestJwtBuilder.DefaultSecret,
            Algorithm = algo,
        };
        Assert.Throws<InvalidValueException>(opts.Validate);
    }

    [Fact]
    public void Validate_rejects_non_positive_response_lifetime()
    {
        var opts = new JsConnectOptions
        {
            ClientId = "c",
            Secret = TestJwtBuilder.DefaultSecret,
            ResponseLifetime = TimeSpan.Zero,
        };
        Assert.Throws<InvalidValueException>(opts.Validate);
    }
}
