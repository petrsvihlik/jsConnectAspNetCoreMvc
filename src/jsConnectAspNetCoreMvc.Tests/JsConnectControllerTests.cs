using jsConnectNetCore;
using Xunit;

namespace jsConnectAspNetCoreMvc.Tests
{
    public class JsConnectControllerTests
    {
        // Currently, the data allowing us to do tests reside in the production instances of Vanilla and Intercom. We should create some in the staging instances, i.e. "https://kentico.vanillastaging.com/".
        private const string API_URI = "https://forums.kenticocloud.com/"; 

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
        [InlineData("janl@kentico.com")]
        [InlineData("branislavs@kentico.com")]
        [InlineData("bryans@kentico.com")]
        [InlineData("chrisj@kentico.com")]
        [InlineData("janap@kentico.com")]
        [InlineData("janc@kentico.com")]
        [InlineData("jurajk@kentico.com")]
        [InlineData("juraju@kentico.com")]
        [InlineData("lukasm@kentico.com")]
        [InlineData("lukasp2@kentico.com")]
        [InlineData("martind2@kentico.com")]
        [InlineData("martinh@kentico.com")]
        [InlineData("martin.michalik@kentico.com")]
        [InlineData("ondrejch@kentico.com")]
        [InlineData("ondrejf2@kentico.com")]
        [InlineData("ondrejs@kentico.com")]
        public async void GetsExistingUser(string email)
        {
            // Arrange
            var client = new VanillaApiClient(API_URI, null);

            // Act
            var user = await client.GetUser(email: email);

            // Assert
            Assert.NotNull(user);
        }

        [Theory]
        [InlineData("0B0AF54F-5A55-4C78-8E86-C31CE3E43F3B")]
        public async void NonExistentUserReturnsNull(string email)
        {
            // Arrange
            var client = new VanillaApiClient(API_URI, null);

            // Act
            var user = await client.GetUser(email: email);

            // Assert
            Assert.Null(user);
        }
    }
}
