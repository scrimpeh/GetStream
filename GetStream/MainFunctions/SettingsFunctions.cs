using System;

using GetStream.Config;

using static GetStream.GetStreamMain;

namespace GetStream.MainFunctions
{
	internal static class SettingsFunctions
	{
		//Loads the settings and transfers them to the program.
		internal static void LoadSettings()
		{
			var success = SettingsManager.LoadSettings(out userSettings);
			if (!success) Console.WriteLine(Strings.set_load_error_msg);

			history = userSettings.channel_history;
			defaultQuality = userSettings.default_quality;
			MediaPlayerInterface.MediaPlayerPath = userSettings.media_player_path;
			MediaPlayerInterface.Settings = userSettings;
		}

		//Serzialize settings object to disk.
		internal static void SaveSettings()
		{
			var success = SettingsManager.SaveSettings();
			if (!success)
			{
				Console.WriteLine(Strings.set_save_error_msg);
			}
		}

		//We expect exactly one tuple: set <setting> <value>, which either 
		//succeeds or returns an error value, but does not try to throw exceptions
		//or anything.
		internal static void ChangeSetting(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine(Strings.set_not_specified_msg);
				return;
			}
			var setting = args[1];
			var value = args.Length > 2 ? args[2] : null;
			var result = SettingsManager.TrySet(setting, value);
			Console.Write(Strings.set_result_msgs[result]);
		}

		internal static void GetSetting(string[] args)
		{
			if (args.Length < 2 || args[1] == "all" || args[1] == "*")
			{
				DisplayFunctions.DisplayAllSettings();
				return;
			}

			var value = SettingsManager.TryGet(args[1]);
			if (value == null)
			{
				Console.WriteLine(Strings.set_not_found_msg);
			}
			else
			{
				Console.WriteLine($"{SettingsManager.GetSettingName(args[1])}: {value}");
				Console.WriteLine(SettingsManager.GetSettingDescription(args[1]));
			}
		}
	}
}
