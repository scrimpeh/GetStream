using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace GetStream
{
	public static class TokenGetter
	{
		//This class contains static methods for accessing tokens from twitch. Or wherever.

		/// <summary>
		/// Obtains a string containing raw web data from a HTTP URL.
		/// </summary>
		/// <param name="httpUrl">The fully qualified HTTP URL to obtain the Json data from.</param>
		/// <param name="timeout">The timeout time to wait for the request. Optional</param>
		/// <returns>A string containing raw web data in JSon Format, if successful.</returns>
		/// <throws>ChannelNotFoundException, if twitch could not find the appropriate channel</throws>
		public static string GetJsonWebData(string httpUrl, int timeout = 2000)
		{
			try 
			{
				return GetRawWebData(httpUrl, timeout);
			}
			catch(WebException we)
			{
				//Convert the exception into a more descriptive exception 
				//for the main program.
				var errorResponse = we.Response as HttpWebResponse;
				if(errorResponse == null) throw;
				switch ((int)errorResponse.StatusCode)
				{
					case 404:
					case 422:
						throw new ChannelNotFoundException(we.Message);
					default:
						throw;
				}
			}
		}

		/// <summary>
		/// Obtain a M3U8 stream playlist using an access token from twitch.
		/// </summary>
		/// <param name="at">The twitch access token</param>
		/// <param name="channelName">The name of the channel<param>
		/// <param name="timeout">The timeout in milliseconds. Optional</param>
		/// <returns>A string object containing the M3U playlist of the stream.</returns>
		public static string GetM3UFromToken(TwitchAccessToken at, string channelName, int timeout = 5000)
		{
			var random = Util.GetTwitchRandomNumber();
			var requestUrl = string.Format(Strings.twitch_usher_url, channelName, at.token, at.sig, random);
			try
			{
				return GetRawWebData(requestUrl, timeout);
			}
			catch (WebException we)
			{
				var errorResponse = we.Response as HttpWebResponse;
				if (errorResponse == null) throw;
				switch ((int)errorResponse.StatusCode)
				{
					case 404:
						throw new ChannelOfflineException();
					default:
						throw;
				}
			}
		}

		/// <summary>
		/// Obtain a M3U8 VOD playlist using an access token from twitch.
		/// </summary>
		/// <param name="at">The twitch access token</param>
		/// <param name="vodID">The ID of the VOD in string form.<param>
		/// <param name="timeout">The timeout in milliseconds. Optional</param>
		/// <returns>A string object containing the M3U playlist of the VOD.</returns>
		public static string GetVodM3UFromToken(TwitchAccessToken at, string vodID, int timeout = 5000)
		{
			string random = Util.GetTwitchRandomNumber();
			string requestUrl = string.Format(Strings.twitch_usher_vod_url, vodID, at.token, at.sig, random);
			try
			{
				return GetRawWebData(requestUrl, timeout);
			}
			catch (WebException we)
			{
				var errorResponse = we.Response as HttpWebResponse;
				if (errorResponse == null) throw;
				switch ((int)errorResponse.StatusCode)
				{
					case 404:
						throw new ChannelOfflineException();
					default:
						throw;
				}
			}
		}

		/// <summary>
		/// Deserializes a Json string containing data into an Access Token for twitch.
		/// </summary>
		/// <param name="json">The string containing Json data.</param>
		/// <returns>The corresponding TwitchAccsesToken to the input string.</returns>
		public static TwitchAccessToken GetTokenFromJson(string json)
		{
			return JsonConvert.DeserializeObject<TwitchAccessToken>(json);
		}

		//Retrieves a string containing data from a website.
		//Does not try to catch any exceptions, error handling is 
		//the job of the calling functions.
		private static string GetRawWebData(string httpUrl, int timeout = 2000)
		{
			var request = GetWebRequest(httpUrl, timeout);
			using (var response = (HttpWebResponse)request.GetResponse())
			using (var dataStream = response.GetResponseStream())
			using (var reader = new StreamReader(dataStream))
			{
				return reader.ReadToEnd();
			}
		}

		// Helper function to create a WebRequest.
		private static WebRequest GetWebRequest(string httpUrl, int timeout)
		{
			var req = WebRequest.Create(httpUrl);
			req.Timeout = timeout;
			req.Credentials = CredentialCache.DefaultCredentials;
			return req;
		}
	}

	/// <summary>
	/// A Basic AccessToken, as returned from Twitch.
	/// </summary>
	public struct TwitchAccessToken
	{
		[JsonRequired]
		public string token { get; private set; }

		[JsonRequired]
		public string sig { get; private set; }

		public bool mobile_restricted { get; private set; }
	}
}
