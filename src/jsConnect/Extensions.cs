using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Globalization;

namespace jsConnect
{
	public static class Extensions
	{
		/// <summary>
		/// Converts given object to JSON with padding.
		/// </summary>
		/// <param name="o">Object to convert to JSONP</param>
		/// <param name="callback">Callback function to wrap the data with</param>
		/// <returns>JSONP string</returns>
		public static string ToJsonp(this object o, string callback)
		{
			if (string.IsNullOrEmpty(callback))
			{
				throw new ArgumentNullException(nameof(callback), "Callback parameter must be initialized.");
			}
			if (o == null)
			{
				throw new ArgumentNullException(nameof(o));
			}
			return $"{callback}({JsonConvert.SerializeObject(o)})";
		}

		/// <summary>
		/// Creates a UNIX timestamp.
		/// </summary>
		/// <param name="dt"><see cref="DateTime"/> object to convert to a timestamp</param>
		public static int Timestamp(this DateTime dt)
		{
			DateTime epoch = new DateTime(1970, 1, 1);
			TimeSpan span = dt - epoch;
			return (int)span.TotalSeconds;
		}

		/// <summary>
		/// Converts byte array to a hexadecimal string
		/// </summary>
		/// <param name="buff">Array to convert.</param>
		public static string ToHexString(this byte[] buff)
		{
			if (buff == null)
			{
				throw new ArgumentNullException(nameof(buff));
			}
			return buff.Aggregate("", (current, t) => current + t.ToString("x2"));
		}

		/// <summary>
		/// Encodes URL according to RFC1738 (http://www.ietf.org/rfc/rfc1738.txt). In addition, it encodes $-_.+!*'() characters.
		/// </summary>
		public static string UrlEncode(this string s)
		{
			if (s == null)
			{
				throw new ArgumentNullException(nameof(s));
			}
            static string me(Match m) => m.ToString().ToUpper();

            string result = WebUtility.UrlEncode(s);
			result = Regex.Replace(result, "%[0-9a-f][0-9a-f]", me);
			// Escape $+!*'() characters, leave -_. unescaped
			result = result.Replace("'", "%27");
			result = result.Replace("!", "%21");
			result = result.Replace("*", "%2A");
			result = result.Replace("(", "%28");
			result = result.Replace(")", "%29");

			return result;
		}

		/// <summary>
		/// Retrieves an object from a given <see cref="Dictionary{TKey,TValue}"/>. Covers situations where the <see cref="key"/> doesn't exist.
		/// </summary>
		/// <returns>Returns value if the given key exists. Otherwise, returns a default value of <see cref="L"/>.</returns>
		public static L GetValue<T, L>(this Dictionary<T, L> dic, T key)
		{
			if (dic == null)
			{
				throw new ArgumentNullException(nameof(dic));
			}
			if (dic.ContainsKey(key))
			{
				return dic[key];
			}
			else
			{
				return default;
			}
		}

        /// <summary>
        /// Removes diacritics from the current string.
        /// </summary>
        public static string RemoveAccents(this string input)
        {
            return new string(
                input
                .Normalize(System.Text.NormalizationForm.FormD)
                .ToCharArray()
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());
        }
    }
}
