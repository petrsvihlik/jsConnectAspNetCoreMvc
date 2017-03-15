using jsConnectNetCore;
using Xunit;

namespace jsConnectAspNetCoreMvc.Tests
{
    public class JsConnectControllerTests
    {
        private const string API_URI = "https://kentico.vanillastaging.com/";

        public JsConnectControllerTests()
        {
        }

        [Theory]
        [InlineData("PetrSvihlik")]
        [InlineData("JanLenoch")]
        [InlineData("MartinDanko")]
        public async void GetsExistingUserName(string fullName)
        {
            // Arrange
            var client = new VanillaApiClient(API_URI, null);

            // Act
            string resultingUserName = await client.GetUniqueUserName(fullName);

            // Assert
            Assert.NotEqual(fullName, resultingUserName);
        }

        [Theory]
        [InlineData("JmenoPrijmeni")]
        public async void GetsNewUserName(string fullName)
        {
            // Arrange
            var client = new VanillaApiClient(API_URI, null);

            // Act
            string resultingUserName = await client.GetUniqueUserName(fullName);

            // Assert
            Assert.Equal("JmenoPrijmeni", resultingUserName);
        }

        [Theory]
        [InlineData("JanLenoch")]
        public async void CreatesNewSuffixedUserName(string fullName)
        {
            // Arrange
            var client = new VanillaApiClient(API_URI, null);

            // Act
            string resultingUserName = await client.GetUniqueUserName(fullName);

            // Assert
            Assert.Equal("JanLenoch1", resultingUserName);
        }

        [Theory]
        [InlineData("0B0AF54F-5A55-4C78-8E86-C31CE3E43F3B")]
        public async void NonExistentUserReturnsNull(string userId)
        {
            // Arrange
            var client = new VanillaApiClient(API_URI, null);

            // Act
            var user = await client.GetUser(userId);

            // Assert
            Assert.Null(user);
        }
    }
}
