using System;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using jsConnectNetCore.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

namespace jsConnectNetCore.Controllers
{
    /// <summary>
    /// Authentication endpoint implemented in accordance with http://docs.vanillaforums.com/help/sso/jsconnect/seamless/.
    /// </summary>
    [Route("[controller]")]
    public class JsConnectController : AbstractControllerBase<JsConnectController>
    {
        #region "Configuration"

        public string ClientId => Configuration.GetValue("Vanilla:ClientID", string.Empty);
        public string ClientSecret => Configuration.GetValue("Vanilla:ClientSecret", string.Empty);
        public int TimestampValidFor => Configuration.GetValue("Vanilla:TimestampValidFor", 30 * 60);
        public bool AllowWhitespaceInUsername => Configuration.GetValue("Vanilla:AllowWhitespaceInUsername", false);
        public string VanillaApiBaseUri => Configuration.GetValue("Vanilla:ApiBaseUri", string.Empty);

        #endregion

        private HashAlgorithm HashAlgorithm { get; set; }


        public JsConnectController(IConfiguration configuration, ILogger<JsConnectController> logger, HashAlgorithm hashAlgorithm) : base(configuration, logger)
        {
            HashAlgorithm = hashAlgorithm;
        }

        /// <summary>
        /// Returns details of a currently signed-in user, if any.
        /// </summary>
        [HttpGet("[action]")]
        [Produces("application/json")]
        public async Task<ActionResult> Authenticate([FromQuery] string client_id, [FromQuery] string callback, [FromQuery] int? timestamp = null, [FromQuery] string signature = null)
        {
            JsConnectResponseModel jsConnectResult = new JsConnectResponseModel();

            try
            {
                ValidateJsIncomingRequest(client_id, timestamp, signature, callback);

                var user = HttpContext.User;

                if (user != null && user.Identity.IsAuthenticated && timestamp.HasValue)
                {
                    //string userName = Task.Run(() => GetUserName(user.FindFirst(ClaimTypes.Name))).Result;

                    string uniqueId = user.FindFirst(ClaimTypes.NameIdentifier).Value;
                    string fullName = user.FindFirst(ClaimTypes.Name).Value;

                    // Sign-in user response
                    jsConnectResult.UniqueId = uniqueId;
                    jsConnectResult.Name = await GetVanillaUserName(uniqueId, fullName);
                    jsConnectResult.Email = user.FindFirst(ClaimTypes.Email).Value;
                    jsConnectResult.PhotoUrl = user.FindFirst("AvatarUrl")?.Value;
                    jsConnectResult.Roles = user.FindFirst("Roles")?.Value;
                }
                else
                {
                    // No user response
                    jsConnectResult.UniqueId = string.Empty;
                    jsConnectResult.Name = string.Empty;
                    jsConnectResult.Email = string.Empty;
                    jsConnectResult.PhotoUrl = string.Empty;
                }

                jsConnectResult.ClientId = ClientId;
                jsConnectResult.Signature = Hash(jsConnectResult.QueryString + ClientSecret);

                return new ContentResult
                {
                    Content = jsConnectResult.ToJsonp(callback),
                    ContentType = "application/javascript",
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                // Error response
                JsConnectException exception = ex as JsConnectException;
                if (exception != null)
                {
                    jsConnectResult.Error = exception.Error;
                }
                jsConnectResult.Message = ex.Message;
                Logger.LogError(new EventId(ex.HResult), ex, ex.Message);

                return new JsonResult(jsConnectResult);
            }
        }

        public async Task<string> GetVanillaUserName(string uniqueId, string fullName)
        {
            string resultingUserName;

            if (!string.IsNullOrEmpty(VanillaApiBaseUri))
            {
                string transformedFullName = Regex.Replace(fullName, @"\s+", "");
                transformedFullName = transformedFullName.RemoveAccents();

                using (var vanillaClient = new VanillaApiClient(VanillaApiBaseUri))
                {
                    resultingUserName = await vanillaClient.GetUserName(uniqueId, transformedFullName);
                }
            }
            else
            {
                throw new Exception("The Vanilla:ApiBaseUri application settings cannot be empty.");
            }

            return resultingUserName;
        }

        /// <summary>
        /// Validates request parameters.
        /// </summary>
        private void ValidateJsIncomingRequest(string clientId, int? timestamp, string timestampHash, string callback)
        {
            if (string.IsNullOrEmpty(callback))
            {
                throw new JsConnectException(JsConnectException.ERROR_INVALID_REQUEST, $"The {nameof(callback)} parameter is missing.");
            }
            if (string.IsNullOrEmpty(clientId))
            {
                throw new JsConnectException(JsConnectException.ERROR_INVALID_REQUEST, "The client_id parameter is missing.");
            }
            else if (clientId != ClientId)
            {
                throw new JsConnectException(JsConnectException.ERROR_INVALID_CLIENT, $"Unknown client {clientId}.");
            }

            if (timestamp.HasValue)
            {
                if (Math.Abs(DateTime.UtcNow.Timestamp() - timestamp.Value) > TimestampValidFor)
                {
                    throw new JsConnectException(JsConnectException.ERROR_INVALID_REQUEST, "The timestamp is expired.");
                }
                if (!IsTimestampValid(timestamp, timestampHash))
                {
                    throw new JsConnectException(JsConnectException.ERROR_INVALID_REQUEST, "The signature is invalid.");
                }
            }
        }

        /// <summary>
        /// Validates timestamp by creating it's signed hash and comparing it to the given hash.
        /// </summary>
        /// <param name="timestamp">Timestamp to validate</param>
        /// <param name="timestampHash">Hash to validate the timestamp against</param>
        private bool IsTimestampValid(int? timestamp, string timestampHash)
        {
            string clientHash = Hash(timestamp + ClientSecret);
            return clientHash == timestampHash;
        }

        /// <summary>
        /// Creates hash of the given content.
        /// </summary>
        /// <param name="content">Content to  hash</param>
        private string Hash(string content)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(content);
            return HashAlgorithm.ComputeHash(textBytes).ToHexString();
        }
    }
}
