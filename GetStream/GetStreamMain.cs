using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using GetStream.Config;
using GetStream.MainFunctions;

namespace GetStream
{
	partial class GetStreamMain
	{
		public static int timeout = 5000; //The timeout in milliseconds
		internal static IList<string> history;
		public static bool quit = false;
		public static bool vod_mode = false;
		public static string channelName = string.Empty;
		public static string currentQuality = string.Empty;
		public static Settings userSettings;
		public static M3U8 currentPlaylist = null;
		public static string defaultQuality = "source";

		[DllImport("Kernel32")]
		public static extern bool SetConsoleCtrlHandler(ExitEventHandler Handler, bool add);

		public delegate void ExitEventHandler(CtrlSig sig);
		static ExitEventHandler exitHandler;

		[STAThread]
		static void Main(string[] args)
		{
			//Trap Ctrl+C, trap exits.
			Console.CancelKeyPress += (o, e) => e.Cancel = true;
			exitHandler += new ExitEventHandler(HandleProgramExit);
			SetConsoleCtrlHandler(exitHandler, true);

			//Read arguments, get settings from file, query the user for settings if various things
			//like media player path are not known.
			Console.WriteLine(Strings.welcome_msg);

			//First load settings, then handle command arguments, in case an argument
			//overrides something in the settings.
			SettingsFunctions.LoadSettings();
			HandleConsoleArguments(args);

			if (!userSettings.media_player_path_known)
			{
				Console.WriteLine(Strings.set_media_player_path_not_known);
			}

			HistoryFunctions.DisplayHistoryStartup();

			IntPtr handle = IntPtr.Zero;
			using (var p = Process.GetCurrentProcess())
			{
				handle = p.Handle;
			}

			while (!quit)
			{
				//The main core of this program will be an infinite loop, at least in console mode.
				//It will intercept user messages and pipe them to an instance of MPC, if open.
				Console.Write("> ");
				string input = Console.ReadLine();
				if(input == null)	//Input cancelled.
				{
					Console.WriteLine();
					continue;
				}
				ExecuteCommand(input);
			}

			HandleProgramExit();
		}

		private static void HandleProgramExit(CtrlSig sig = CtrlSig.CTRL_CLOSE_EVENT)
		{
			//Cleanup code, try to save settings, and so on.
			switch(sig) {
				case CtrlSig.CTRL_C_EVENT:
				case CtrlSig.CTRL_BREAK_EVENT:
					//Do nothing here. This is a dummy call for
					//when the user calls Ctrl+C, which does -not-
					//exit the program
					break;
				case CtrlSig.CTRL_CLOSE_EVENT:
				case CtrlSig.CTRL_LOGOFF_EVENT:
				case CtrlSig.CTRL_SHUTDOWN_EVENT:
				default:
					SettingsFunctions.SaveSettings();
					if (userSettings.exitMPOnQuit && MediaPlayerInterface.MediaPlayerOpen)
						MediaPlayerInterface.CloseMediaPlayer();
					break;
			}
        }

		private static void HandleConsoleArguments(string[] args)
		{
			var currentCommand = new StringBuilder(string.Empty);
			char[] commandTypes = { '/', '!', '?' };

			//Collate individual arguments to commands
			for(int i = 0; i < args.Length; i++)
			{
				var currentWord = $"\"{args[i]}\" ";		//Enclose all arguments in quotes, just to be sure.
				if(commandTypes.Contains(currentWord[1]))	//Execute the previous command.
				{
					var command = currentCommand.ToString();
					currentCommand.Clear();
					ExecuteCommand(command);
				}
				currentCommand.Append(currentWord);
			}
			if(currentCommand.Length > 0) 
			{
				ExecuteCommand(currentCommand.ToString());
			}
		}

		private static void ExecuteCommand(string command)
		{
			command = command.Trim();
			command = command.ToLower();	//The input can either be a command (prepended by a '/') with arguments, or a channel.
											//Parsing a channel without quality option will prompt the user to select a quality,
											//alternatively, the desired quality can be appended as an argument using "-h, -m, -l, -cellphone_mode")
											//Ideally, there's a history that can be accessed by [1], [2], [3], and so forth.
			if (command == string.Empty) return;
			//Split string by spaces, treat sections enclosed in quotes as one
			var input_args = command.SplitOnQuotes();
			var command_type = input_args[0][0];

			switch (command_type)
			{
				case '/':   //Execute a command
					HandleCommand(input_args);
					break;
				case '-':   //Load a new quality iff a stream is already loaded
					SetQuality(input_args);
					break;
				case '!':   //Try setting an option - shorthand for /set <option> <value>
				case '?':
					AccessSetting(input_args, command_type);
					break;
				case '<':
				case '>':
					bool up = command_type == '>';
					var newQuality = ParseFunctions.GetNextQuality(up);
					var openStreamArgs = new [] { "/o", newQuality };
					ParseFunctions.OpenStream(openStreamArgs);
					break;
				default:
					ParseOther(input_args);
                break;
            }
		}

		private static void HandleCommand(string[] args)
		{
			var command_type = args[0].Substring(1);
			var qCommand = commandAssocs.FindAlias(command_type);
			if(qCommand == null)
			{
				Console.WriteLine(string.Format(Strings.unrecognized_command_msg,args[0]));
				return;
			}
			commands[qCommand](args);   //Now run the command. 
		}

		private static void SetQuality(string[] args)
		{
			if (currentPlaylist != null)
			{
				var quality = ParseFunctions.GetQuality(args[0].Substring(1), true);
				if (quality == null)
				{
					Console.WriteLine(string.Format(Strings.invalid_quality_msg, args[0]));
				}
				else
				{
					var openStreamArgs = new List<string>() { "/o", quality };
					openStreamArgs.AddRange(args.Skip(1).Select(Util.TrimSlashes));
					ParseFunctions.OpenStream(openStreamArgs.ToArray());
				}
			}
			else
			{
				Console.WriteLine(Strings.no_stream_open_msg);
			}
		}

		private static void ParseOther(string[] args)
		{
			//Is the user trying to change the quality
			//note that u, s, h, m, l, mo and a are potentially hiding valid twitch channel names.
			//in case these need to be accessed, type /p normally.
			var input = args[0];
			if (qualityShorthand.Keys.Contains(input) && currentPlaylist != null)
			{
				var quality = qualityShorthand[input];
				currentQuality = quality;
				var openStreamArgs = new List<string>() { "/o", quality };
				openStreamArgs.AddRange(args.Skip(1).Select(Util.TrimSlashes));
				Console.WriteLine(string.Format(Strings.using_quality_msg, quality));
				ParseFunctions.OpenStream(openStreamArgs.ToArray());
			}
			else
			{
				const string isVodRegex = @"(?x) ^((\S*[\\/])? v[\\/])? \d{3,} (\?t=\S+)? [\\/]*$";
				bool parseVod = Regex.IsMatch(input, isVodRegex);
				var parseArgs = new List<string>() { parseVod ? "/v" : "/p" };
				parseArgs.AddRange(args);
				if (parseVod)
					ParseFunctions.ParseVod(parseArgs.ToArray());
				else
					ParseFunctions.ParseChannel(parseArgs.ToArray());
			}
		}

		private static void AccessSetting(string[] args, char commandType)
		{
			var set_args = new List<string>() { $"/{commandType}" };
			var setting = args[0].Substring(1);
			if (setting != string.Empty) set_args.Add(setting);
			set_args.AddRange(args.Skip(1));
			if (commandType == '?')
				SettingsFunctions.GetSetting(set_args.ToArray());
			else
				SettingsFunctions.ChangeSetting(set_args.ToArray());
		}

		private static void StopProgramLoop(string[] args)
		{
			quit = true;
		}
	}
}
