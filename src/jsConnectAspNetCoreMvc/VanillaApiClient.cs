using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using jsConnectNetCore.Models;
using System.Text.RegularExpressions;

namespace jsConnectNetCore
{
    public class VanillaApiClient : IDisposable
    {
        private string USERS_ENDPOINT = "users/get.json";

        private HttpClient _httpClient;
        private string _vanillaApiUri;

        public HttpClient HttpClient
        {
            get
            {
                return _httpClient ?? (new HttpClient());
            }
        }

        protected ILogger<VanillaApiClient> Logger { get; set; }

        public VanillaApiClient(string vanillaApiUri) //, ILogger<VanillaApiClient> logger)
        {
            if (vanillaApiUri == null)
            {
                throw new ArgumentNullException(nameof(vanillaApiUri));
            }

            if (vanillaApiUri == string.Empty)
            {
                throw new ArgumentException("The vanillaApiUri must not be empty.", nameof(vanillaApiUri));
            }

            _vanillaApiUri = vanillaApiUri;
            //Logger = logger;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        public async Task<string> GetUserName(string userId, string fullName)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("The userId parameter must not be null or an empty string.");
            }

            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException("The fullName parameter must not be null or an empty string.");
            }

            // Ask Vanilla if a user with the userId exists and return their existing .Profile.Name
            var user = await GetUser(userId: userId);

            // TODO: Check if Vanilla distinguishes users in a case-sensitive manner.
            if (!fullName.Equals(user?.Profile?.Name, StringComparison.OrdinalIgnoreCase))
            {
                return user.Profile.Name;
            }
            else
            {
                int x = 1;
                string suffix = string.Empty;
                string suffixedUserName;

                do
                {
                    suffixedUserName = fullName + suffix;
                    user = await GetUser(userName: suffixedUserName);
                    suffix = x.ToString();
                    x++;
                }
                while (user != null);

                return suffixedUserName;
            }
        }

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

            var request = CreateRequest($"{USERS_ENDPOINT}?User.{parameterName}={userName}");

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
                Logger.LogError(new EventId(ex.HResult), ex, ex.Message);
                throw;
            }
        }

        private HttpRequestMessage CreateRequest(string endpoint)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri = new Uri(_vanillaApiUri + endpoint),
                Method = HttpMethod.Get
            };

            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            return request;
        }
    }
}
