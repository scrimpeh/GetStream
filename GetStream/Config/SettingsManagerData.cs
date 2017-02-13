using System;
using System.Collections.Generic;

namespace GetStream.Config
{
	static partial class SettingsManager
	{
		private static IDictionary<string, IEnumerable<string>> settingsAliases =
			new Dictionary<string, IEnumerable<string>>()
		{
			{ "path", new[] { "path", "mediaplayerpath", "p" } },
			{ "default_quality", new[] { "quality", "defautquality", "dq", "q" } },
			{ "clipboard", new[] {"clipboard", "cb", "c", "board" } },
			{ "mediaplayer", new[] { "mediaplayer", "mp" } },
			{ "monitor", new[] { "monitor", "screen", "m" } },
			{ "fullscreen", new[] { "fullscreen", "fs", "f", "full" } },
			{ "exitmponquit", new[] { "exitmponquit", "e" } }
		};

		private static IDictionary<string, string> settingsDescriptions =
			new Dictionary<string, string>()
		{
			{ "path", "Path to the used Media Player" },
			{ "default_quality", "The default quality setting to run streams at" },
			{ "clipboard", "Pass the stream URL to clipboard" },
			{ "mediaplayer", "Open a Media Player to display the stream in" },
			{ "monitor", "Which monitor to open the Media Player on by default" },
			{ "fullscreen", "Open the Media Player in fullscreen by default" },
			{ "exitmponquit", "Exit the media player when quitting the program" }
		};

		internal static IDictionary<bool, IEnumerable<string>> boolAssocs =
			new Dictionary<bool, IEnumerable<string>>()
		{
			{ true, new[] { "true", "on", "1", "yes" } },
			{ false, new[] { "false", "off", "0", "no" } }
		};

		internal static IEnumerable<string> toggleAssocs = new List<string>() { "toggle", "t", "flip" };

		internal static IDictionary<string, Func<string, TrySetResult>> setters =
			new Dictionary<string, Func<string, TrySetResult>>()
		{
			{ "path", SetPath },
			{ "default_quality", SetDefQuality },
			{ "clipboard", SetCB },
			{ "mediaplayer", SetMP },
			{ "monitor", SetMonitor },
			{ "fullscreen", SetFullscreen },
			{ "exitmponquit", SetExitMPOnQuit }
		};

		/// <summary>
		/// An enum containing the results of trying to change a setting.
		/// </summary>
		public enum TrySetResult
		{
			Success,
			SettingNotFound,
			InvalidValue,
			InvalidValueType,
			NoValueSpecified,
			FileNotFound
		};
	}
}
