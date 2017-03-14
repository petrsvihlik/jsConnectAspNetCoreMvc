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

        private HttpClient _httpClient = null;
        private string _vanillaApiUri;

        public HttpClient HttpClient
        {
            get
            {
                return _httpClient ?? (new HttpClient());
            }
        }

        protected ILogger<VanillaApiClient> Logger { get; set; }

        public VanillaApiClient(string vanillaApiUri, ILogger<VanillaApiClient> logger)
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
            Logger = logger;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        /// <summary>
        /// Gets a user name of either an existing or a new user. Should the proposed name conflict with another user's one, a numeric suffix is added.
        /// </summary>
        /// <param name="userId">Unique ID of the Vanilla user</param>
        /// <param name="fullName">The proposed user name</param>
        /// <returns>The unique user name</returns>
        public async Task<string> GetUniqueUserName(string userId, string fullName)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("The userId parameter must not be null or an empty string.");
            }

            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException("The fullName parameter must not be null or an empty string.");
            }

            var user = await GetUser(userId: userId);

            // TODO: Check if Vanilla distinguishes users in a case-sensitive manner. Asked in https://kenticosoftware.sharepoint.com/sites/ConsultingProjects/VanillaForums/Lists/Backlog/Flat.aspx?RootFolder=%2Fsites%2FConsultingProjects%2FVanillaForums%2FLists%2FBacklog%2FQ%20Are%20user%20names%20case%20sensitive&FolderCTID=0x01200200DCD59A83F9BF31498745D74175F3CE51
            if (fullName.Equals(user?.Profile?.Name, StringComparison.OrdinalIgnoreCase))
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

            var request = CreateRequest($"{USERS_ENDPOINT}?User.{parameterName}={parameterValue}");

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
                Logger?.LogError(new EventId(ex.HResult), ex, ex.Message);
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
