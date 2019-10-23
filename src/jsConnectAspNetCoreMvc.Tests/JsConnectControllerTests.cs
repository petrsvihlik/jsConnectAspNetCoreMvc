using jsConnectNetCore;
using Xunit;

namespace jsConnectAspNetCoreMvc.Tests
{
    public class JsConnectControllerTests
    {
        /// <summary>
        /// Point this to your forum instance.
        /// </summary>
        private const string VANILLA_API_URI = "https://forums.yourdomain.tld/"; 

        public JsConnectControllerTests()
        {
        }

        [Theory]
        [InlineData("ExistingUserName")]
        public async void GetsExistingUserName(string fullName)
        {
            // Arrange
            using var client = new VanillaApiClient(VANILLA_API_URI, null);

            // Act
            string resultingUserName = await client.GetUniqueUserName(fullName);

            // Assert
            Assert.NotEqual(fullName, resultingUserName);
        }

        [Theory]
        [InlineData("NameSurname")]
        public async void GetsNewUserName(string fullName)
        {
            // Arrange
            using var client = new VanillaApiClient(VANILLA_API_URI, null);

            // Act
            string resultingUserName = await client.GetUniqueUserName(fullName);

            // Assert
            Assert.Equal("NameSurname", resultingUserName);
        }

        [Theory]
        [InlineData("JohnDoe")]
        public async void CreatesNewSuffixedUserName(string fullName)
        {
            // Arrange
            using var client = new VanillaApiClient(VANILLA_API_URI, null);

            // Act
            string resultingUserName = await client.GetUniqueUserName(fullName);

            // Assert
            Assert.Equal("JohnDoe1", resultingUserName);
        }

        [Theory]
        [InlineData("existing_user@domain.tld")]
        public async void GetsExistingUser(string email)
        {
            // Arrange
            using var client = new VanillaApiClient(VANILLA_API_URI, null);

            // Act
            var user = await client.GetUser(email: email);

            // Assert
            Assert.NotNull(user);
        }

        [Theory]
        [InlineData("non_existing_user@domain.tld")]
        public async void NonExistentUserReturnsNull(string email)
        {
            // Arrange
            using var client = new VanillaApiClient(VANILLA_API_URI, null);

            // Act
            var user = await client.GetUser(email: email);

            // Assert
            Assert.Null(user);
        }
    }
}
