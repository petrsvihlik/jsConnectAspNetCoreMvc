using System;

namespace jsConnectNetCore
{
	/// <summary>
	/// Exception object containing parameters expected by Vanilla Forums.
	/// </summary>
	public class JsConnectException : Exception
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
		public string Error { get; set; }

		/// <summary>
		/// Default constructor.
		/// </summary>
		public JsConnectException(string error, string message) : base(message)
		{
			Error = error;
		}
	}
}
