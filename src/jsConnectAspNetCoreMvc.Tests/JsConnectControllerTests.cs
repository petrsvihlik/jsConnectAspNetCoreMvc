using jsConnectNetCore.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Xunit;
using Moq;

namespace jsConnectAspNetCoreMvc.Tests
{
    public class JsConnectControllerTests
    {
        public static IConfigurationRoot Configuration { get; set; }
        public static ILogger<JsConnectController> Logger { get; set; }

        public JsConnectControllerTests()
        {
            var dict = new Dictionary<string, string>
            {
                { "Vanilla:ApiBaseUri", "https://kentico.vanillastaging.com/api/v1/" }
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(dict);
            Configuration = builder.Build();
            var loggerMock = new Mock<ILogger<JsConnectController>>();

            // Not working since extension methods are static, hence not-mockable.
            //loggerMock.Setup(m => m.LogError(It.IsAny<EventId>(), It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()));

            Logger = loggerMock.Object;
        }

        [Theory]
        [InlineData("4", "PetrSvihlik")]
        [InlineData("12", "JanLenoch")]
        [InlineData("20", "MartinDanko")]
        public async void GetsExistingUserName(string uniqueId, string fullName)
        {
            // Arrange
            var controller = new JsConnectController(Configuration, Logger, SHA512.Create());

            // Act
            string resultingUserName = await controller.GetVanillaUserName(uniqueId, fullName);

            // Assert
            Assert.Equal(fullName, resultingUserName);
        }

        [Theory]
        [InlineData("21", "Jméno Příjmení 01")]
        public async void GetsNewUserName(string uniqueId, string fullName)
        {
            // Arrange
            var controller = new JsConnectController(Configuration, Logger, SHA512.Create());

            // Act
            string resultingUserName = await controller.GetVanillaUserName(uniqueId, fullName);

            // Assert
            Assert.Equal("JmenoPrijmeni01", resultingUserName);
        }

        [Theory]
        [InlineData("21", "JanLenoch")]
        public async void CreatesNewSuffixedUserName(string uniqueId, string fullName)
        {
            // Arrange
            var controller = new JsConnectController(Configuration, Logger, SHA512.Create());

            // Act
            string resultingUserName = await controller.GetVanillaUserName(uniqueId, fullName);

            // Assert
            Assert.Equal("JanLenoch1", resultingUserName);
        }
    }
}
