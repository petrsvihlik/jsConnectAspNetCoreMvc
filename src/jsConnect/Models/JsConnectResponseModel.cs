using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace jsConnect.Models
{
	public class JsConnectResponseModel
	{
		private string _error = null;
		private string _message = null;

		[JsonProperty("client_id")]
		public string ClientId
		{
			get; set;
		}

		[JsonProperty("signature")]
		public string Signature
		{
			get; set;
		}


		[JsonProperty("error")]
		public string Error
		{
			get { return _error; }
			set
			{
				UserData.Clear();
				_error = value;
			}
		}


		[JsonProperty("message")]
		public string Message
		{
			get { return _message; }
			set
			{
				UserData.Clear();
				_message = value;
			}
		}

		#region "User Data"

		[JsonIgnore]
		private readonly Dictionary<string, string> UserData = [];

		[JsonProperty("uniqueid")]
		public string UniqueId
		{
			get { return UserData.GetValue(nameof(UniqueId)); }
			set
			{
				if (value != null)
				{
					UserData[nameof(UniqueId)] = value;
				}
			}
		}


		[JsonProperty("name")]
		public string Name
		{
			get { return UserData.GetValue(nameof(Name)); }
			set
			{
				if (value != null)
				{
					UserData[nameof(Name)] = value;
				}
			}
		}


		[JsonProperty("email")]
		public string Email
		{
			get { return UserData.GetValue(nameof(Email)); }
			set
			{
				if (value != null)
				{
					UserData[nameof(Email)] = value;
				}
			}
		}


		[JsonProperty("photourl")]
		public string PhotoUrl
		{
			get { return UserData.GetValue(nameof(PhotoUrl)); }
			set
			{
				if (value != null)
				{
					UserData[nameof(PhotoUrl)] = value;
				}
			}
		}


		[JsonProperty("roles")]
		public string Roles
		{
			get { return UserData.GetValue(nameof(Roles)); }
			set
			{
				if (value != null)
				{
					UserData[nameof(Roles)] = value;
				}
			}
		}

		[JsonIgnore]
		public string QueryString
		{
			get
			{
				return string.Join("&", UserData.OrderBy(p => p.Key).Select(kv => kv.Key.ToLower().UrlEncode() + "=" + kv.Value.UrlEncode()));
			}
		}

		#endregion
	}
}
