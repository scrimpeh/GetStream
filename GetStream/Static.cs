using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using GetStream.Config;

namespace GetStream
{
	public static class Strings
	{
		public const string welcome_msg = "Welcome to GetStream! Type /h for help. Type /q for quit.";

		public const string channel_found_msg = "Found channel {0} to parse...";

		public const string twitch_access_token_url = "http://api.twitch.tv/api/channels/{0}/access_token.json?as3=t";

		public const string twitch_vod_url = "http://api.twitch.tv/api/vods/{0}/access_token.json?as3=t";

		public const string twitch_usher_url =
			"http://usher.twitch.tv/api/channel/hls/{0}.m3u8?player=twitchweb" +
			"&token={1}&sig={2}&allow_audio_only=true&allow_source=true&allow_spectre=false" + 
			"&type=any&p={3}";

		public const string twitch_usher_vod_url =
			"http://usher.twitch.tv/vod/{0}?player=twitchweb" +
			"&token={1}&sig={2}&allow_audio_only=true&allow_source=true&allow_spectre=false" +
			"&type=any&p={3}";

		public const string settings_path = "settings.json";

		#region errors
		public static IDictionary<Type, string> channel_parse_error_messages = new Dictionary<Type, string>()
		{
			{ typeof(URINotFoundException), "Couldn't find URL. What website are you trying to parse from?" },
			{ typeof(ChannelOfflineException), "Channel is offline." },
			{ typeof(ChannelNotFoundException), "Couldn't find channel or VOD to parse. Check your spelling." },
			{ typeof(VODNotFoundException), "Couldn't find the VOD URL. Check your spelling." },
            { typeof(Exception), "Uncaught error. Couldn't parse channel" }
		};
		public const string channel_parse_error_unknown = "Unknown error. Couldn't parse channel";

		public const string no_stream_open_msg = "No stream open yet! Try opening one first by entering the channel!";
		public const string unrecognized_command_msg = "Could not recognize command {0}, type /h for help!";
		public const string no_arguments_msg = "No arguments specified! Type /h for command reference!";
		public const string media_player_not_ready_msg = "The media player hasn't opened yet! Please wait a second and repeat your command!";
		#endregion

		#region hist
		public const string hist_identifier = "[{0}] {1}";
		public const string no_channels_in_hist_msg = "No channels in history.";
		public const string hist_doesnt_exist_msg = "History entry [{0}] doesn't exist!";
		public const string invalid_hist_msg = "Trying to access invalid history entry!";
		public const string hist_msg = "Your previously watched channels: ";
		public const string hist_got_channel_msg = "Parsing channel {0}";
		#endregion

		#region helptopics
		//Associate a command with an array of helptopics.
		private static readonly string[] quit_help =
		{
			"/quit", "Exits the program",
			"Closes the program and returns to desktop.",
			"Saves the user history and various settings to \"settings.json\" in the program directory."
		};

		private static readonly string[] usage_help =
		{
			"/usage", "Displays program usage", 
			"Prints basic information about program usage. New users are recommended reading this before use."
		};

		private static readonly string[] help_help =
		{
			"/help <command?>", "Displays a list of help topics",
			"If <command> is specified, a detailed usage reference for this command will be printed out.",
			"Otherwise, a list of all usable commands with short descriptions will be displayed.",
			"",
			"When used with a command, the command is displayed along with a list of valid arguments, enclosed",
			"in angle brackets. Optional parameters are noted with a \'?\' sigil, multiple arguments",
			"are enclosed in double brackets: \'<< >>\'"
		};

		private static readonly string[] parse_help =
		{
			"/parse <channel> <<arguments>>", 
			"Parses a channel from twitch.tv",
			"Tries to read and parse a twitch channel from either a twitch.tv URL, the channel name itself, or",
			"the user's viewing history, indexed by a number from 1 to 10. Surround twitch channel names with spaces with quotes.",
			"Using /parse is optional, just typing in the channel name or twitch URL will result in an invocation of this command.",
			"", "If successful, further action will be initiated based on the arguments supplied with the command:", "",
			"     <quality> : source, high, medium, low, mobile, audioonly - Sets the quality to open the stream in.",
			"                 If none is specified use the user's default quality. ",
			"   <clipboard> : clipboard, cb - Paste the stream URL into the clipboard.",
			" <mediaplayer> : mediaplayer, mp - Start an instance of MPC-HC and automatically open the stream.",
			"  <fullscreen> : fullscreen, fs - If opening a media player, open it in fullscreen",
			"    <windowed> : windowed, nofs - If opening a media player, open it windowed",
			"     <monitor> : 1, 2, 3, 4 - When opening a media player, open it on the selected monitor from 1 to 4." ,
			"", "If neither <mediaplayer> nor <clipboard> are specified, use the user's default option from the settings."
		};

		private static readonly string[] vod_help =
		{
			"/parse <vod> <<arguments>>", "Parses a VOD from twitch.tv",
			"Tries to parse a VOD from a twitch.tv URL or a video ID. Can optionally given a timestamp in the",
			"the form \'1234567?t=1h2m30s\' to open a VOD at a specific starting time.",
			"Using /vod is optional, typing in a valid video ID will automatically invoke this command.",
			"", "If successful, further action will be initiated based on the arguments supplied with the command", "",
			"     <quality> : source, high, medium, low, mobile, audioonly - Sets the quality to open the stream in.",
			"                 If none is specified use the user's default quality. ",
			"   <clipboard> : clipboard, cb - Paste the stream URL into the clipboard.",
			" <mediaplayer> : mediaplayer, mp - Start an instance of MPC-HC and automatically open the stream.",
			"  <fullscreen> : fullscreen, fs - If opening a media player, open it in fullscreen",
			"    <windowed> : windowed, nofs - If opening a media player, open it windowed",
			"     <monitor> : 1, 2, 3, 4 - When opening a media player, open it on the selected monitor from 1 to 4.",
			"", "If neither <mediaplayer> nor <clipboard> are specified, use the user's default option from the settings."
		};

		private static readonly string[] history_help =
		{
			"/history", "Displays the user's history",
			"Displays the last ten channels the user visited."
		};

		private static readonly string[] clrhist_help =
		{
			"/clrhist", "Clears the user history",
			"When executed, the user's history will be wiped and set back to zero."
		};

		private static readonly string[] status_help =
		{
			"/status", "Displays status",
			"Display information about the current program status, what channel you're watching, ",
			"and what quality options are available. Also displays all settings that the user has ",
			"currently set."
		};

		private static readonly string[] set_help =
		{
			"/set <setting> <value?>", "Sets a config setting",
			"Sets a option in the setting. Note that !<setting> is shorthand for /set <setting>.",
			"", "Available settings:", 
			"mediaplayerpath - path, p: The path of the media player to use on disk. Note that only MPC-HC",
			"                           is supported as of yet.",
			"     default_quality - dq: The default quality to parse the stream at if no quality is specified.",
			"                           Defaults to source. Can be set to \'source\', \'high\', \'medium\', \'low\'", 
			"                           \'mobile\' and \'audioonly\'",
			"           clipboard - cb: Paste stream links to clipboard by default. Set to \'true\' or \'false\'",
            "         mediaplayer - mp: Open stream links in MPC-HC by default. Set to \'true\' or \'false\'",
            "              monitor - m: If opening a stream in MPC-HC, open it on this monitor by default. Set from 1 to 4.",
			"      fullscreen - fs - f: If opening a stream in MPC-HC, open it in fullscreen by default. Set to \'true\' or \'false\'",
			"           exitonquit - e: Close an open instance of MPC-HC when exiting the program. Set to \'true\' or \'false\'"
		};

		private static readonly string[] get_help =
		{
			"/get <setting?>", "Shows a config setting",
			"Shows the state of the queried option on screen.",
			"Use /? to show all settings the program keeps track of.",
			"Available settings:",
			"     default_quality - dq: The default quality to parse the stream at if no quality is specified.",
			"           clipboard - cb: Paste stream links to clipboard by default.",
			"         mediaplayer - mp: Open stream links in MPC-HC by default.",
			"              monitor - m: If opening a stream in MPC-HC, open it on this monitor by default.",
			"      fullscreen - fs - f: If opening a stream in MPC-HC, open it in fullscreen by default.",
			"           exitonquit - e: Close an open instance of MPC-HC when exiting the program."
		};

		public static IDictionary<string, string[]> help_topics = new Dictionary<string, string[]>()
		{
			{ "usage", usage_help },
			{ "help", help_help },
			{ "parse", parse_help },
			{ "vod", vod_help },
			{ "history", history_help },
			{ "clrhist", clrhist_help },
			{ "status", status_help },
			{ "set", set_help },
			{ "get", get_help },
			{ "quit", quit_help }
		};

		public const string command_ref = "GetStream command reference:\n";
		public const string help_command_not_found = "Couldn't find command <{0}>! Check your spelling!";
		public const string help_aliases = "Aliases: ";

		public static readonly string[] usage_msg =
		{
			"GetStream Usage", "",
			"Paste a twitch.tv URL, twitch channel name or twitch VOD ID into the command line and",
			"automatically open an instance of MPC-HC to play the video. Twitch VODs can be timestamped",
			"to play back the video from the specified starting time.", "",
			"Many commands and settings have short form arguments. If /parse works as a command, so does /p",
			"Check the command reference (/h) for a list of commands and their arguments", "",
			"GetStream supports the following hotkeys as well:",
			"Ctrl+Alt+C: Remove current line",
			"Ctrl+Alt+W, Ctrl+Alt+Q: Quit GetStream",
			"Ctrl+Alt+Up, Ctrl+Alt+Down: Select the next highest/lowest quality if a stream is open",
			"",
			"Currently, only twitch.tv is supported as a video service. Only MPC-HC is supported as a media player.",
			"Before using this application, a path to the media player needs to be set. Use !path \"<pathname>\" for this,",
			"where <pathname> is the path to the media player, enclosed in quotes.", "",
			"GetStream can also be used from the command line. Simply supply commands as typed into the",
			"prompt as arguments and enclose multi-word commands in quotes \"\". To quit GetStream after usage", 
			"type \"/q\" as the final argument.","",
			"Example usage:", 
			">GetStream.exe \"!path \"C:\\MPC\\mpc-hc.exe\" \"/p gamesdonequick -source -mp\" \"/q\"", "",
			"GetStream keeps a settings file in \"settings.json\" in the program's directory, in which permanent",
			"settings are saved. Use /! and /? to change and view settings, respectively. To wipe settings, ",
			"delete the file on disk.", ""
		};
		#endregion

		#region quality
		public const string unknown_quality_msg = "Unknown quality option specified. Use -s, -m, -l, -mo, or -a!";
		public const string no_quality_msg = "No quality options specified";
		public const string quality_not_available_msg = "Quality option {0} not available";
        public const string using_default_quality_msg = "{0} - Trying default quality option: {1}";
		public const string using_highest_quality_msg = "{0} - Using highest available quality option: {1}";
		public const string using_quality_msg = "Using quality option: {0}";
		public const string invalid_quality_msg = "Picked invalid quality option: {0} - Use -s, -m, -l, -mo or -a!";
		#endregion	

		#region status messages
		public const string status_header = "GetStream v.1.0";
		public const string status_connected = "Currently connected to channel: {0}";
		public const string status_vod = "Currently watching a VOD.";
		public const string status_quality_options = "Available quality options: ";
		public const string status_not_connected = "Currently no channel is open.";
		public const string status_cur_quality = "Currently selected quality: {0}";
		public const string status_forced_clipboard_mode = "No media player path known. Therefore copying results to clipboard."
		+ "\nTo open a media player, write !path \"<pathname>\"\n";
		public const string status_clipboard_mode = "Currently copying results to clipboard.\n";
		public const string status_media_player_open = "An instance of MPC is currently running.\n";
		#endregion

		public const string clipboard_msg = "In clipboard: {0}";

		#region settings
		public static IDictionary<SettingsManager.TrySetResult, string> set_result_msgs = 
			new Dictionary<SettingsManager.TrySetResult, string>()
		{
			{ SettingsManager.TrySetResult.Success, string.Empty },
			{ SettingsManager.TrySetResult.SettingNotFound, "Option not found! Check your spelling!\n" },
			{ SettingsManager.TrySetResult.InvalidValueType, "Couldn't recognize this value!\n" },
			{ SettingsManager.TrySetResult.InvalidValue, "Invalid value set!\n" },
			{ SettingsManager.TrySetResult.NoValueSpecified, "Didn't specify a value!\n" },
			{ SettingsManager.TrySetResult.FileNotFound, "Path not found or invalid file specified.\n" }
		};

		public const string set_invalid_msg = "Didn't specify the right setting! Use /h set for a command reference!";
		public const string set_not_specified_msg = "No setting specified!";
		public const string set_not_found_msg = "No setting with that name found!";
		public const string set = "\nSettings: ";

		public const string set_load_error_msg = "Couldn't load settings from disk!\nUsing default settings.";
		public const string set_save_error_msg = "Couldn't save settings to disk!";
		public const string set_media_player_path_not_known = "\nNo path to media player specified.\n" +
		"Can only copy media URLs to clipboard until path is set.\n" +
		"Type !path <pathname> to set path.";
		#endregion
	}

	//Utility Functions and Extension Functions and the like
	public static class Util
	{
		public const int maxMonitorCount = 4;

		public static string GetTwitchRandomNumber()
		{
			return ((int)( new Random().NextDouble() * 999999 )).ToString();
		}

		/// <summary>
		/// Returns the item that the alias refers to.
		/// </summary>
		/// <typeparam name="TKey">The key of the alias</typeparam>
		/// <typeparam name="T">The type of the alias</typeparam>
		/// <param name="dict">The dictionary to look in.</param>
		/// <param name="alias">The alias to check.</param>
		/// <returns>The key the alias refers to if present, or null if not.</returns>
		public static TKey FindAlias<TKey, T>(this IDictionary<TKey, IEnumerable<T>> dict, T alias)
		{
			return dict.FirstOrDefault(e => e.Value.Contains(alias)).Key;
		}

		/// <summary>
		/// Tries to find the item the alias refers to, and returns if it was successful.
		/// </summary>
		/// <typeparam name="TKey">The key of the alias</typeparam>
		/// <typeparam name="T">The type of the alias</typeparam>
		/// <param name="dict">The dictionary to check</param>
		/// <param name="alias">The alias to check.</param>
		/// <param name="result">The result to write into</param>
		/// <returns>True, if successful, false if not.</returns>
		public static bool TryFindAlias<TKey, T>(this IDictionary<TKey, IEnumerable<T>> dict, T alias, out TKey result)
		{
			if( dict.Any(e => e.Value.Contains(alias)) )
			{ 
				result = dict.First(e => e.Value.Contains(alias)).Key;
				return true;
			}
			result = default(TKey);
			return false;
		}

		public static string[] SplitOnQuotes(this string input)
		{
			var regex = @"[\""].+?[\""]|[^ ]+";
			return Regex.Matches(input, regex)
				.Cast<Match>()
				.Select(m => m.Value.Trim('"'))
				.ToArray();
		}

		public static string[] SplitOnQuotes(this string input, params char[] separator)
		{
			var sb = new StringBuilder(separator.Length);
			foreach (var ch in separator)
			{
				sb.Append(ch);
			}
			return Regex.Matches(input, $@"[\""].+?[\""]|[^{sb.ToString()}]+")
				.Cast<Match>()
				.Select(m => m.Value.Trim('"'))
				.ToArray();
        }

		/// <summary>
		/// Returns the alias for the quality setting
		/// </summary>
		/// <returns>The quality setting aliased by the input, or null if not found.</returns>
		public static bool GetQualityAlias<TKey>(KeyValuePair<TKey, IEnumerable<string>> kvp, string searchFor)
			=> kvp.Value.First().StartsWith(searchFor) || kvp.Value.Skip(1).Contains(searchFor);

		/// <summary>
		/// Trim leading and trailing hyphens, slashes and other undesired characters from a string.
		/// </summary>
		/// <param name="input">The string to trim.</param>
		/// <returns>The input string, trimmed of any leading and trailing slashes and hyphens.</returns>
		public static string TrimSlashes(string input) => input.Trim('/', '-');

		public static IDictionary<ConsoleModifiers, int> WindowsKeyCodes
			= new Dictionary<ConsoleModifiers, int>()
		{
			{ ConsoleModifiers.Alt, 0xA4 },
			{ ConsoleModifiers.Control, 0x11 },
			{ ConsoleModifiers.Shift, 0x10 }
		};
	}

	public enum CtrlSig
	{
		CTRL_C_EVENT = 0,
		CTRL_BREAK_EVENT = 1,
		CTRL_CLOSE_EVENT = 2,
		CTRL_LOGOFF_EVENT = 5,
		CTRL_SHUTDOWN_EVENT = 6
	}

	//Maybe useful?
	public enum OutputType
	{
		Null = 0,
		Print = 1,
		Clipboard = 2,
		MediaPlayer = 3
	};

	#region WIN32 stuff
	/// <summary>
	/// A basic .NET Apprxomination of the W32 COPYDATASTRUCT
	/// struct. Contains three members:
	/// dwData: ULONGPTR, in our case used as a value for parsing command indices.
	/// cbData: DWORD used to incidate the length of lpData, unused for us
	/// lpData: PVOID used to get MPC-HC a string representation of the arguments.
	/// Mind that this struct has to compile differently depending on target architecture,
	/// and on 64 bits will require the members be padded twice as much.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
#if x86
	public unsafe struct COPYDATASTRUCT
	{
		[FieldOffset(0)]
		public ulong dwData;
		[FieldOffset(4)]
		public int cbData;
		[FieldOffset(8)]
		public void* lpData;
	}
#elif x64
		public unsafe struct COPYDATASTRUCT
		{
			[FieldOffset(0)] public ulong dwData;
			[FieldOffset(8)] public int cbData;
			[FieldOffset(16)] public void* lpData;
		}
#endif
	#endregion
}
