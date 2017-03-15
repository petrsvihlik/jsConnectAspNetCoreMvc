using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using jsConnectNetCore.Models;

namespace jsConnectNetCore
{
    public class VanillaApiClient : IDisposable
    {
        protected readonly HttpClient _httpClient = null;
        protected readonly string _apiBaseUri;
        protected readonly ILogger<VanillaApiClient> _logger;

        protected HttpClient HttpClient => _httpClient ?? new HttpClient();


        public VanillaApiClient(string vanillaApiBaseUri, ILogger<VanillaApiClient> logger)
        {
            if (string.IsNullOrEmpty(vanillaApiBaseUri))
            {
                throw new ArgumentException("The vanillaApiUri must not be empty.", nameof(vanillaApiBaseUri));
            }

            _apiBaseUri = vanillaApiBaseUri;
            _logger = logger;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }


        /// <summary>
        /// Gets an unused username. Should the proposed name be in conflict with another user, a numeric suffix is added.
        /// </summary>
        /// <param name="fullName">The proposed user name</param>
        /// <returns>The unique user name</returns>
        public async Task<string> GetUniqueUserName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException("The fullName parameter must not be null or an empty string.");
            }
            int x = 1;
            string suffix = string.Empty;
            string suffixedUserName;
            VanillaUser user;
            do
            {
                suffixedUserName = fullName + suffix;
                user = await GetUser(userName: suffixedUserName);
                suffix = x.ToString();
                x++;
            } while (user != null);

            return suffixedUserName;
        }


        /// <summary>
        /// Gets a Vanilla user, either by the unique ID or by a user name eventually.
        /// </summary>
        /// <param name="userId">Unique ID of the Vanilla user</param>
        /// <param name="userName">The user name to search by</param>
        /// <returns>The <see cref="VanillaUser"/> user</returns>
        public async Task<VanillaUser> GetUser(string userId = null, string userName = null)
        {
            string parameterName;
            string parameterValue;

            if (!string.IsNullOrEmpty(userId))
            {
                parameterName = "UserId";
                parameterValue = userId;
            }
            else if (!string.IsNullOrEmpty(userName))
            {
                parameterName = "Name";
                parameterValue = userName;
            }
            else
            {
                throw new ArgumentException("Either the userId or userName parameter must be supplied.");
            }

            var request = CreateRequest($"api/v1/users/get.json?User.{parameterName}={parameterValue}");

            try
            {
                HttpResponseMessage response = await HttpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<VanillaUser>(responseContent);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                // The Logger is not guaranteed to be populated (not null), hence the null-coallescence.
                _logger?.LogError(new EventId(ex.HResult), ex, ex.Message);
                throw;
            }
        }

        private HttpRequestMessage CreateRequest(string uriSuffix)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri = new Uri(_apiBaseUri + uriSuffix),
                Method = HttpMethod.Get
            };

            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            return request;
        }
    }
}
