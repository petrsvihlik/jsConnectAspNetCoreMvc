using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Configuration;
using jsConnectNetCore.Controllers;
using System.Security.Cryptography;

namespace jsConnectAspNetCoreMvc.Tests
{
    public class JsConnectControllerTests
    {
        public static IConfigurationRoot Configuration { get; set; }

        public JsConnectControllerTests()
        {
            var dict = new Dictionary<string, string>
            {
                { "Vanilla:ApiBaseUri", "https://kentico.vanillastaging.com/api/v1/" }
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(dict);

            Configuration = builder.Build();
        }

        [Theory]
        [InlineData("4", "PetrSvihlik")]
        [InlineData("12", "JanLenoch")]
        [InlineData("20", "MartinDanko")]
        public async void GetsExistingUserName(string uniqueId, string fullName)
        {
            // Arrange
            var controller = new JsConnectController(Configuration, null, SHA512.Create());

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
            var controller = new JsConnectController(Configuration, null, SHA512.Create());

            // Act
            string resultingUserName = await controller.GetVanillaUserName(uniqueId, fullName);

            // Assert
            Assert.Equal("JmenoPrijmeni01", resultingUserName);
        }

        [Theory]
        [InlineData("22", "JmenoPrijmeni01")]
        public async void CreatesNewSuffixedUserName(string uniqueId, string fullName)
        {
            // Arrange
            var controller = new JsConnectController(Configuration, null, SHA512.Create());

            // Act
            string resultingUserName = await controller.GetVanillaUserName(uniqueId, fullName);

            // Assert
            Assert.Equal("JmenoPrijmeni01", resultingUserName);
        }
    }
}
