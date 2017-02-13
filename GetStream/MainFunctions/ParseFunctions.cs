using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using GetStream.Config;

using static GetStream.GetStreamMain;

namespace GetStream.MainFunctions
{
	//Class containing parsing methods and such.
	internal static class ParseFunctions
	{
		// Tries to parse a channel and get a usable stream URL.
		// If the right argument is specified, it will then try to pipe
		// the result into MPC or something.
		internal static void ParseChannel(string[] args)
		{
			//First off, if a history shortcut is specified, nab that from the
			//history list.
			if (args.Length <= 1)
			{
				Console.WriteLine(Strings.no_arguments_msg);
				return;
			}

			//Step 1: Get full channel name
			var tempChannelName = GetFullChannelName(args[1]);

			if (tempChannelName == null) return;

			var qualifiedArgs = args.Select(Util.TrimSlashes).ToList();
			var qualifiedUrl = string.Format(Strings.twitch_access_token_url, tempChannelName);
			var access_token = new TwitchAccessToken();

			//Step 2: Get access token
			try
			{
				var access_token_json = TokenGetter.GetJsonWebData(qualifiedUrl, timeout);
				access_token = TokenGetter.GetTokenFromJson(access_token_json);
			}
			catch (Exception e)
			{
				Console.WriteLine(GetParseErrorMessage(e.GetType()));
				HistoryFunctions.RemoveFromHistory(tempChannelName);
				return;
			}

			//If we got this far, the channel was parsable, so it exists - may still be offline.
			if (!qualifiedArgs.Contains("nohist"))
			{
				HistoryFunctions.AddToHistory(tempChannelName);
			}

			string m3u;
			try
			{
				m3u = TokenGetter.GetM3UFromToken(access_token, tempChannelName, timeout);
			}
			catch (Exception e)
			{
				Console.WriteLine(GetParseErrorMessage(e.GetType()));
				return;
			}

			//Now we have a playlist, time to parse it and pipe it further.
			channelName = tempChannelName;
			Console.WriteLine(string.Format(Strings.channel_found_msg, channelName));
			currentPlaylist = M3U8Parser.ParseM3U8(m3u);

			//Get desired quality and further stream args, then try to actually open
			//the stream.
			vod_mode = false;
			OpenStream(PrepareLaunchArgs(qualifiedArgs));
		}

		//To parse a vod, the user almost certainly wants to paste a full video url
		//into the chat window.
		//Annoyingly, twitch gives us very little information about VODs when parsing them,
		//things like length, author or title are not served - if we wanted them,
		//we'd have to pull them out of the URLs and tokens that we get, and not everything is available.
		internal static void ParseVod(string[] args)
		{
			if (args.Length <= 1)
			{
				Console.WriteLine(Strings.no_arguments_msg);
				return;
			}

			//Step 1: Get twitch's vod ID
			var trimmedUrl = args[1].TrimEnd('/', '\\');            //Remove any URL remnants
			var vodArgs = trimmedUrl.Split('/', '\\').Last().Split('?');
			var vodID = vodArgs[0]; //Now look for arguments, most notably timestamps.
			TimeSpan? timestamp = null;
			foreach (var arg in vodArgs.Skip(1))
			{
				switch (arg[0])
				{
					case 't':   //We got a timestamp, now parse it.
						var groups = Regex.Match(arg, @"^t=(?:(\d*)h)?(?:(\d*)m)?(?:(\d*)s)?").Groups;
						var ts = new int[3];
						for (var i = 0; i < 3; i++)
							int.TryParse(groups[i + 1].ToString(), out ts[i]);
						timestamp = new TimeSpan(ts[0], ts[1], ts[2]);
						break;
					default:
						break;
				}
			}

			var qualifiedArgs = args.Select(Util.TrimSlashes).ToList();
			var twitchVodApiUrl = string.Format(Strings.twitch_vod_url, vodID);
			var vod_access_token = new TwitchAccessToken();

			try
			{
				var vod_access_token_json = TokenGetter.GetJsonWebData(twitchVodApiUrl, timeout);
				vod_access_token = TokenGetter.GetTokenFromJson(vod_access_token_json);
			}
			catch (Exception e)
			{
				Console.WriteLine(GetParseErrorMessage(e.GetType()));
				return;
			}

			string m3u;
			try
			{
				m3u = TokenGetter.GetVodM3UFromToken(vod_access_token, vodID);
			}
			catch (Exception e)
			{
				Console.WriteLine(GetParseErrorMessage(e.GetType()));
				return;
			}
			vod_mode = true;
			currentPlaylist = M3U8Parser.ParseM3U8(m3u);
			OpenStream(PrepareLaunchArgs(qualifiedArgs), timestamp);
		}

		//Tries to open a stream, either tries to launch a MediaPlayer and/or send
		//the stream URL to the media player, or tries to send the input to the clipboard.
		//The third argument specifies whether to open a media player or clipboard.
		//If not supplied, the option specified in the settings will be used.
		//
		//Right now, this is not tied to a user command, instead only being called directly
		//from the main thread or the parse Method. If this is reworked to be tied to a user command
		//"o", argument checking will have to be done!
		internal static void OpenStream(string[] args, TimeSpan? timestamp = null)
		{
			//At this point, all input should have already been sanitized.
			//At this point, either open a Media Player/send it to the open media player,
			//or send it to clipboard.
			var mediaURL = currentPlaylist.MediaLinks[args[1]].MediaLink;
			var arguments = OpenStreamHandleArguments(args);

			if (!userSettings.media_player_path_known || arguments.Item1)
			{
				var displayName = mediaURL.Length < 150 ? mediaURL : $"{mediaURL.Substring(0, 150)}...";
				Console.WriteLine(string.Format(Strings.clipboard_msg, displayName));
				Clipboard.SetText(mediaURL);
			}
			else
			{
				var mpLaunchArgs = MediaPlayerInterface.GetDefaultArgs();
				mpLaunchArgs.FullScreen = arguments.Item2;
				mpLaunchArgs.Monitor = arguments.Item3;
				if (timestamp.HasValue)
				{
					mpLaunchArgs.StartTime = timestamp.Value;
				}
				MediaPlayerInterface.OpenMediaPlayer(mediaURL, mpLaunchArgs);
			}
		}

		private static string[] PrepareLaunchArgs(ICollection<string> args)
		{
			var quality = args.FirstOrDefault(arg =>
				qualityAssocs.FirstOrDefault(e =>
				Util.GetQualityAlias(e, arg)).Key != null);
			args.Remove(quality);
			quality = GetQuality(quality);
			var prepared_args = new List<string>() { "/o", quality };
			prepared_args.AddRange(args.Skip(2));  //Add any remaining arguments
			return prepared_args.ToArray();
		}

		//Try to get the name of a channel from either the history 
		//or what have you. channel may not be null
		private static string GetFullChannelName(string channel)
		{
			//If the channel resembles a history entry
			if (Regex.IsMatch(channel, @"^\[?\d*\]?\z"))
			{   //Try to read the channel from history.
				int histnum;
				int.TryParse(channel.Trim('[', ']'), out histnum);
				if (histnum <= history.Count && histnum > 0)
				{
					var result = history[histnum - 1];
					Console.WriteLine(string.Format(Strings.hist_got_channel_msg, result));
					return result;
				}
				else
				{
					string error_msg = (histnum <= 0 || histnum > Settings.maxhist) ?
						Strings.invalid_hist_msg :
						string.Format(Strings.hist_doesnt_exist_msg, histnum);
					Console.WriteLine(error_msg);
					return null;
				}
			}
			else //Trim the http stuff, if the channel is a url
			{
				var trimmedUrl = channel.TrimEnd('/', '\\');            //Remove any URL remnants
				var components = trimmedUrl.Split('/', '\\');           //if there are any     
				return Regex.Replace(components.Last(), @"\s+", "");    //if the user supplied a channel name, trim spaces
			}
		}

		//Tries to get the best available quality option from the playlist.
		//If the stream doesn't offer the desired quality, the next highest is chosen.
		//if returnNullOnInvalidQuality is set, instead of trying the default quality,
		//null is returned
		internal static string GetQuality(string quality, bool returnNullOnInvalidQuality = false)
		{
			//The highest possible quality the stream offers.
			var availableQualities = currentPlaylist.MediaLinks.Keys;
			var highestQuality = availableQualities.First();

			if (string.IsNullOrWhiteSpace(quality))
			{
				if (returnNullOnInvalidQuality)
					return null;

				Console.WriteLine(string.Format(Strings.using_default_quality_msg, Strings.no_quality_msg, defaultQuality));
				quality = defaultQuality;
			}

			var qOption = qualityAssocs.FirstOrDefault(e => Util.GetQualityAlias(e, quality)).Key;
			if (qOption == null)
			{
				if (returnNullOnInvalidQuality)
					return null;

				Console.WriteLine(string.Format(Strings.using_default_quality_msg, Strings.unknown_quality_msg, defaultQuality));
				qOption = defaultQuality;
			}
			//We now got the desired quality option. Try to find the highest 
			//quality option that matches.

			if (availableQualities.Contains(qOption))
			{
				Console.WriteLine(string.Format(Strings.using_quality_msg, qOption));
				currentQuality = qOption;
				return qOption;
			}
			else
			{
				//return the next highest quality option, or the highest available as a fallback
				//if nothing else works.
				var remainingQualityOptions = qualityAssocs.Keys
					.SkipWhile(opt => opt != qOption)
					.Except(new[] { "audioonly" }); //if we want audio only, explicitely state it.

				currentQuality = remainingQualityOptions.FirstOrDefault(
					opt => availableQualities.Contains(opt)) ?? highestQuality;

				Console.WriteLine(string.Format(
					Strings.using_highest_quality_msg,
					string.Format(Strings.quality_not_available_msg, qOption), highestQuality));

				return currentQuality;
			}
		}

		//If up, get the next higher quality,
		//if not up, get the next lower quality
		//The exception is audioonly - we don't want the user
		//to accidentally select this
		internal static string GetNextQuality(bool up)
		{
			if (currentPlaylist != null && currentQuality != "audioonly")
			{
				var availableQualities = currentPlaylist.MediaLinks.Keys.
					Except(new[] { "audioonly" }).ToList();
				var index = availableQualities.IndexOf(currentQuality);
				var newQuality = currentQuality;
				if(up && index > 0)
				{ 
					newQuality = availableQualities[index - 1];
					Console.WriteLine(string.Format(Strings.using_quality_msg, newQuality));
				}
				else if(!up && index < availableQualities.Count - 1)
				{
					newQuality = availableQualities[index + 1];
					Console.WriteLine(string.Format(Strings.using_quality_msg, newQuality));
				}
				currentQuality = newQuality;
			}

			return currentQuality;
		}

		/// <summary>
		/// Get a Tuple containing launch args.
		/// </summary>
		/// <param name="args">The arguments to handle.</param>
		/// <returns>A Tuple containing whether or not to use clipboard mode. It also contains 
		/// info about fullscreen and monitor to use if not.</returns>
		private static Tuple<bool, bool, int> OpenStreamHandleArguments(string[] args)
		{
			var clipboard = userSettings.copyToClipBoardMode;
			var fullscreen = userSettings.launchFullscreen;
			var monitor = userSettings.launchOnMonitor;
			for (int i = 2; i < args.Length; i++)
			{
				var argument = streamLaunchArgAliases.FindAlias(args[i]);
				switch (argument)
				{
					case "mediaplayer":
						clipboard = false;
						break;
					case "clipboard":
						clipboard = true;
						break;
					case "fullscreen":
						fullscreen = true;
						break;
					case "windowed":
						fullscreen = false;
						break;
					case "monitor":
						//This is "1, 2, 3, 4", because I don't expect to need any more numeric arguments
						//or that anyone using this will have more than 4 monitors.
						int newMonitor;
						if (int.TryParse(args[i], out newMonitor)) monitor = newMonitor;
						break;
				}
			}
			return new Tuple<bool, bool, int>(clipboard, fullscreen, monitor);
		}

		/// <summary>
		/// Helper Function, looks up the appropriate error message for a given exception type.
		/// </summary>
		/// <param name="exceptionName">Name of the exception</param>
		/// <returns>A string containing an error message.</returns>
		private static string GetParseErrorMessage(Type exType)
		{
			string ret;
			Strings.channel_parse_error_messages.TryGetValue(exType, out ret);
			return ret ?? Strings.channel_parse_error_unknown;
		}
	}
}
