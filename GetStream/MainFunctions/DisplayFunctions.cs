using System;
using System.Linq;

using GetStream.Config;

using static GetStream.GetStreamMain;

namespace GetStream.MainFunctions
{
	internal static class DisplayFunctions
	{
		internal static void DisplayHelp(string[] args)
		{
			if (args.Length == 1)
			{
				ListCommands();
			}
			else
			{
				var qualifiedArg = Util.TrimSlashes(args[1]);
				var qCommand = commandAssocs.FindAlias(qualifiedArg) ?? "";
				string[] helpMessage;
				if (Strings.help_topics.TryGetValue(qCommand, out helpMessage))
				{
					var aliases = commandAssocs[qCommand];
					Console.WriteLine("{0}:\n{1}\n", qCommand.ToUpper(), helpMessage[0]);

					Console.WriteLine(Strings.help_aliases);
					Console.Write(aliases.First());
					foreach (var alias in aliases.Skip(1))
					{
						Console.Write(", {0}", alias);
					}
					Console.WriteLine("\n");

					foreach (var line in helpMessage.Skip(2))
					{
						Console.WriteLine(line);
					}
					Console.WriteLine();
				}
				else
				{
					Console.WriteLine(string.Format(Strings.help_command_not_found, args[1]));
				}
			}
		}

		private static void ListCommands()
		{
			Console.WriteLine(Strings.command_ref);
			foreach (var kvp in Strings.help_topics)
			{
				Console.WriteLine("{0}: {1}", kvp.Key, kvp.Value[1]);
			}
			Console.WriteLine();
		}

		//Displays info about the user info.
		internal static void DisplayStatus(string[] args)
		{
			Console.WriteLine(Strings.status_header);
			if (currentPlaylist != null)
			{
				if (vod_mode) Console.WriteLine(Strings.status_vod);
				else Console.WriteLine(string.Format(Strings.status_connected, channelName));
				Console.WriteLine(Strings.status_quality_options);
				DisplayAvailableQualities();
				Console.WriteLine(string.Format(Strings.status_cur_quality, qualityNameAssocs[currentQuality]));
			}
			else
			{
				Console.WriteLine(Strings.status_not_connected);
			}

			var clipboardmode_msg = !userSettings.media_player_path_known ?
			Strings.status_forced_clipboard_mode : userSettings.copyToClipBoardMode ?
			Strings.status_clipboard_mode : MediaPlayerInterface.MediaPlayerOpen ?
			Strings.status_media_player_open : string.Empty ;
			Console.Write(clipboardmode_msg);

			Console.WriteLine(Strings.set);
			DisplayAllSettings();
		}

		internal static void DisplayAvailableQualities()
		{
			var availableQualities = currentPlaylist.MediaLinks.Values;
			foreach (var tuple in availableQualities)
			{
				Console.WriteLine(tuple.ToString());
			}
		}

		internal static void DisplayAllSettings()
		{
			var setDict = SettingsManager.GetAllSettings();
			foreach (var kvp in setDict)
			{
				Console.WriteLine($"{kvp.Key}: {kvp.Value.ToString()}");
			}
			Console.WriteLine();
		}

		//Prints a string to console that explains the program usage to the user.
		internal static void DisplayUsage(string[] args)
		{
			foreach(var s in Strings.usage_msg) 
			{
				Console.WriteLine(s);
			}
		}
	}
}
