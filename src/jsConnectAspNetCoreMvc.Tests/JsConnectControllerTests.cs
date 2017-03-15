using jsConnectNetCore;
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
        private const string API_URI = "https://kentico.vanillastaging.com/api/v1/";
        private const bool ALLOW_WHITESPACE = false;

        private ILogger _logger;

        public JsConnectControllerTests()
        {
            var loggerMock = new Mock<ILogger<VanillaApiClient>>();

            // Not working since extension methods are static, hence not-mockable. We would have to use MS Fakes' so called 'shim' for redirecting such a method call. Fakes are still not available in core.
            //loggerMock.Setup(m => m.LogError(It.IsAny<EventId>(), It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()));

            _logger = loggerMock.Object;
        }

        [Theory]
        [InlineData("4", "PetrSvihlik")]
        [InlineData("12", "JanLenoch")]
        [InlineData("20", "MartinDanko")]
        public async void GetsExistingUserName(string uniqueId, string fullName)
        {
            // Arrange
            var client = new VanillaApiClient(API_URI, ALLOW_WHITESPACE, null);

            // Act
            string resultingUserName = await client.GetNormalizedUserName(uniqueId, fullName);

            // Assert
            Assert.Equal(fullName, resultingUserName);
        }

        [Theory]
        [InlineData("21", "Jméno Příjmení 01")]
        public async void GetsNewUserName(string uniqueId, string fullName)
        {
            // Arrange
            var client = new VanillaApiClient(API_URI, ALLOW_WHITESPACE, null);

            // Act
            string resultingUserName = await client.GetNormalizedUserName(uniqueId, fullName);

            // Assert
            Assert.Equal("JmenoPrijmeni01", resultingUserName);
        }

        [Theory]
        [InlineData("21", "JanLenoch")]
        public async void CreatesNewSuffixedUserName(string uniqueId, string fullName)
        {
            // Arrange
            var client = new VanillaApiClient(API_URI, ALLOW_WHITESPACE, null);

            // Act
            string resultingUserName = await client.GetNormalizedUserName(uniqueId, fullName);

            // Assert
            Assert.Equal("JanLenoch1", resultingUserName);
        }
    }
}
