using System;

namespace jsConnect
{
    /// <summary>
    /// Exception object containing parameters expected by Vanilla Forums.
    /// </summary>
    /// <remarks>
    /// Default constructor.
    /// </remarks>
    public class JsConnectException(string error, string message) : Exception(message)
	{
		#region "Preconfigured error codes"

		/// <summary>
		/// The request contains invalid data.
		/// </summary>
		public const string ERROR_INVALID_REQUEST = "invalid_request";

		/// <summary>
		/// The client parameter doesn't match.
		/// </summary>
		public const string ERROR_INVALID_CLIENT = "invalid_client";

        #endregion

        /// <summary>
        /// Short error code.
        /// </summary>
        public string Error { get; set; } = error;
    }
}
