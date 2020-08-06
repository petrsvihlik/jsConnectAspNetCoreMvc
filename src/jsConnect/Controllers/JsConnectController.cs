﻿using System;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using jsConnect;
using jsConnect.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace jsConnect.Controllers
{
    /// <summary>
    /// Authentication endpoint implemented in accordance with http://docs.vanillaforums.com/help/sso/jsconnect/seamless/.
    /// </summary>
    [Route("[controller]")]
    public class JsConnectController : AbstractControllerBase<JsConnectController>
    {
        #region "Configuration"

        /// <summary>
        /// Client ID is used to identify a Vanilla instance and is public.
        /// </summary>
        public string ClientId => Configuration.GetValue("Vanilla:ClientID", string.Empty);

        /// <summary>
        /// Secret is like a site-password of a Vanilla instance and must be kept secret.
        /// </summary>
        public string ClientSecret => Configuration.GetValue("Vanilla:ClientSecret", string.Empty);

        /// <summary>
        /// For secure requests, Vanilla will send you the timestamp of its request. 
        /// If you are checking security than you want to make sure that the timestamp is not too old. 
        /// A recommended timeframe is about 5-30 minutes.
        /// </summary>
        public int TimestampValidFor => Configuration.GetValue("Vanilla:TimestampValidFor", 30 * 60);

        /// <summary>
        /// If false, whitespaces are removed from usernames.
        /// </summary>
        public bool AllowWhitespaceInUsername => Configuration.GetValue("Vanilla:AllowWhitespaceInUsername", false);

        /// <summary>
        /// If false, accented characters are replaced with non-accented chars.
        /// </summary>
        public bool AllowAccentsInUsername => Configuration.GetValue("Vanilla:AllowAccentsInUsername", false);

        /// <summary>
        /// If false, a unique username will be generated by finding and appending a suitable suffix.
        /// </summary>
        public bool AllowDuplicateUserNames => Configuration.GetValue("Vanilla:AllowDuplicateUserNames", false);

        /// <summary>
        /// Base Vanilla API URI. Example: https://forums.domain.tld/
        /// </summary>
        public string BaseUri => Configuration.GetValue("Vanilla:BaseUri", string.Empty);

        #endregion

        private HashAlgorithm HashAlgorithm { get; set; }


        public JsConnectController(IConfiguration configuration, ILogger<JsConnectController> logger, ILoggerFactory loggerFactory, HashAlgorithm hashAlgorithm) : base(configuration, logger, loggerFactory)
        {
            HashAlgorithm = hashAlgorithm;
        }

        /// <summary>
        /// Returns details of a currently signed-in user, if any.
        /// </summary>
        [HttpGet("[action]")]
        [Produces("application/json")]
        public async Task<ActionResult> AuthenticateAsync([FromQuery] string client_id, [FromQuery] string callback, [FromQuery] int? timestamp = null, [FromQuery] string signature = null)
        {
            JsConnectResponseModel jsConnectResult = new JsConnectResponseModel();

            try
            {
                ValidateJsIncomingRequest(client_id, timestamp, signature, callback);

                var user = HttpContext.User;

                if (user != null && user.Identity.IsAuthenticated && timestamp.HasValue)
                {
                    string uniqueId = user.FindFirst(ClaimTypes.NameIdentifier).Value;
                    string email = user.FindFirst(ClaimTypes.Email).Value;
                    string fullName = user.FindFirst(ClaimTypes.Name).Value;

                    // Sign-in user response
                    jsConnectResult.UniqueId = uniqueId;
                    jsConnectResult.Name = await GetVanillaUsername(email, fullName);
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
                if (ex is JsConnectException exception)
                {
                    jsConnectResult.Error = exception.Error;
                }
                jsConnectResult.Message = ex.Message;
                Logger.LogError(new EventId(ex.HResult), ex, ex.Message);

                return new JsonResult(jsConnectResult);
            }
        }

        private async Task<string> GetVanillaUsername(string email, string fullName)
        {
            var logger = LoggerFactory?.CreateLogger<VanillaApiClient>();

            string resultingUserName = fullName;

            using (var vanillaClient = new VanillaApiClient(BaseUri, logger))
            {
                // Try to get user
                var vanillaUser = await vanillaClient.GetUser(email: email);
                if (vanillaUser != null)
                {
                    // Existing user (don't change username)
                    resultingUserName = vanillaUser.Profile.Name;
                }
                else
                {
                    // New user (generate a new username based on settings)
                    if (!AllowWhitespaceInUsername)
                    {
                        resultingUserName = Regex.Replace(resultingUserName, @"\s+", "");
                    }
                    if (!AllowAccentsInUsername)
                    {
                        resultingUserName = resultingUserName.RemoveAccents();
                    }
                    if (!AllowDuplicateUserNames)
                    {
                        resultingUserName = await vanillaClient.GetUniqueUserName(resultingUserName);
                    }
                }
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