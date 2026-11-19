using jsConnect.AspNetCore;
using jsConnect.Exceptions;
using jsConnect.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace jsConnect.Tests.AspNetCore;

public class JsConnectServiceCollectionExtensionsTests
{
    private static IConfiguration Section(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build().GetSection("Vanilla");

    [Fact]
    public void AddJsConnect_binds_options_from_a_configuration_section()
    {
        var section = Section(new()
        {
            ["Vanilla:ClientId"] = TestJwtBuilder.DefaultClientId,
            ["Vanilla:Secret"] = TestJwtBuilder.DefaultSecret,
        });

        using var provider = new ServiceCollection().AddJsConnect(section).BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<JsConnectV3>());
        Assert.IsType<ClaimsJsConnectUserResolver>(provider.GetRequiredService<IJsConnectUserResolver>());
    }

    [Fact]
    public void AddJsConnect_with_an_invalid_configuration_section_fails_on_resolve()
    {
        var section = Section(new() { ["Vanilla:ClientId"] = "c" });

        using var provider = new ServiceCollection().AddJsConnect(section).BuildServiceProvider();

        Assert.Throws<InvalidValueException>(() => provider.GetRequiredService<JsConnectV3>());
    }

    [Fact]
    public void AddJsConnect_rejects_null_arguments()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddJsConnect((IConfiguration)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddJsConnect((Action<JsConnectOptions>)null!));
    }
}
