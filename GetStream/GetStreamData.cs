using System;
using System.Collections.Generic;

using GetStream.MainFunctions;

namespace GetStream
{
	//Here go dictionaries and command references and the like.
	partial class GetStreamMain
	{
		//Dictionaries and so forth.
		//Contains shorthand aliases for the atomic commands the program understands.
		//It is very important that the lists used as values be disjoint.
		public static IDictionary<string, IEnumerable<string>> commandAssocs = new Dictionary<string, IEnumerable<string>>()
		{
			{ "quit", new[] { "quit", "q", "exit" } },
			{ "help", new[] { "help", "h", "man", "manual" } },
			{ "parse", new[] { "parse", "p", "stream", "s" } },
			{ "vod", new[] { "vod", "v", "getvod" } },
			{ "history", new[] { "history", "hist", "prev", "last", "l" } },
			{ "clrhist", new[] { "clrhist", "clearhistory", "clh", "c" } },
			{ "usage", new[] { "usage", "u", "use" } },
			{ "status", new[] { "status", "info", "i" } },
			{ "set", new[] { "set", "!" } },
			{ "get", new[] { "get", "?" } },
		};

		//This should have exactly the same keys as the above dictionary.
		public static IDictionary<string, Action<string[]>> commands = new Dictionary<string, Action<string[]>>()
		{
			{ "quit", StopProgramLoop },
			{ "help", DisplayFunctions.DisplayHelp },
			{ "parse", ParseFunctions.ParseChannel },
			{ "vod", ParseFunctions.ParseVod },
			{ "history", HistoryFunctions.DisplayHistory },
			{ "clrhist", HistoryFunctions.ClearHistory },
			{ "usage", DisplayFunctions.DisplayUsage },
			{ "status", DisplayFunctions.DisplayStatus },
			{ "set", SettingsFunctions.ChangeSetting },
			{ "get", SettingsFunctions.GetSetting }
		};

		//Associates the quality options to a string. Note that the first entry of the value list
		//will be considered as a prefix, that is to say, it is enough to supply any prefix of it
		//to get it to recognize this as the chosen quality options.
		//E.g. "medium" can also be accssed with "m", "me", "med" and so on.
		//Due to the order these entries are put in the dictionary, "m" prioritizes "medium" over "mobile"
		public static IDictionary<string, IEnumerable<string>> qualityAssocs = new Dictionary<string, IEnumerable<string>>()
		{
			{ "source", new[] { "source", "chunked", "ultrahigh", "ultra", "u", "highest", "veryhigh" } },
			{ "high", new[] { "high" } },
			{ "medium", new[] { "medium" } },
			{ "low", new[] { "low" } },
			{ "mobile", new[] { "mobile", "cellphone", "ultralow", "lowest" } },
			{ "audioonly", new[] { "audioonly", "novideo", "audio_only", "audio-only" } }
		};

		//Used for quicker quality changing from the command line.
		public static IDictionary<string, string> qualityShorthand = new Dictionary<string, string>()
		{
			{ "u", "source" }, { "s", "source" },
			{ "h", "high" }, { "hi", "high" },
			{ "m", "medium" }, { "med", "medium" },
			{ "l", "low" }, { "lo", "low" },
			{ "mo", "mobile" }, { "a", "audioonly" }
		};

		public static IDictionary<string, string> qualityNameAssocs = new Dictionary<string, string>()
		{
			{ "source", "Source" }, { "high", "High" },
			{ "medium", "Medium" }, { "low", "Low" },
			{ "mobile", "Mobile" }, { "audioonly", "Audio Only" }
		};

		public static IDictionary<string, IEnumerable<string>> streamLaunchArgAliases = new Dictionary<string, IEnumerable<string>>()
		{
			{ "mediaplayer", new[] { "mediaplayer", "mp" } },
			{ "clipboard", new[] { "clipboard", "cb", "board" } },
			{ "fullscreen", new[] { "fullscreen", "full", "fs", "maximize", "max", "f" } },
			{ "windowed", new[] { "windowed", "w", "win", "nofs", "nofull", "nofullscreen" } },
			{ "monitor", new[] { "1", "2", "3", "4" } },
		};
    }
}
