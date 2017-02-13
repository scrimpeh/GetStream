using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using System.IO;

namespace GetStream.Config
{
	public static partial class SettingsManager
	{
		//This object will be static and act as a interface for the settings object for the main program.
		//While an actual settings object will contain the actual settings.

		//The settings - It's important that these are always passed by ref so that
		//both the main program and the manager are looking at the same value.
		private static Settings UserSettings { get; set; }

		private static string SettingsPath 
		{
			get 
			{ 
				return Path.Combine(
					Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), 
					Strings.settings_path); 
			}
		}
		
		#region IO
		/// <summary>
		/// Loads the settings from disk or creates a new settings object if it couldn't.
		/// </summary>
		/// <param name="settings">The settings object to declare.</param>
		/// <returns>True, if the settings could successfully be loaded from disk or were created anew,
		/// false otherwise.</returns>
		public static bool LoadSettings(out Settings settings)
		{
			var success = false;
			if (File.Exists(SettingsPath))
			{
				string settings_json = null;
				try
				{
					using (var reader = new StreamReader(SettingsPath))
					{
						settings_json = reader.ReadToEnd();
					}

					try
					{
						UserSettings = JsonConvert.DeserializeObject<Settings>(settings_json);
						success = true;
					}
					catch (JsonReaderException) //Something went terribly wrong beforehand.
					{
						success = false;
						UserSettings = new Settings();
					}
				}
				catch (Exception)
				{
					success = false;
				}
			}
			else
			{
				UserSettings = new Settings();
				success = true;
			}
			ValidateSettings();
			settings = UserSettings;
			SaveSettings();
			return success;
		}

		/// <summary>
		/// Semantic analysis of the settings we got dumped from disk.
		/// Restore defaults if something is amiss.
		/// To be called only once when the settings are instantiated
		/// any other error checking should be before the settings are applied.
		/// </summary>
		private static void ValidateSettings()
		{
			//Sanitize history
			UserSettings.channel_history = 
				UserSettings.channel_history.Distinct().Take(Settings.maxhist).ToList();

			//Sanitize default quality
			var defQuality = GetStreamMain.qualityAssocs.FirstOrDefault(e =>
				Util.GetQualityAlias(e, UserSettings.default_quality)).Key ?? "source";
			UserSettings.default_quality = defQuality;

			//Sanitize monitor
			var screen = UserSettings.launchOnMonitor;
			if(screen < 0 || screen >= Util.maxMonitorCount)
				UserSettings.launchOnMonitor = 1;

			//Check if the path exists
			if(!IsValidMediaPlayerPath(UserSettings.media_player_path))
			{
				UserSettings.media_player_path = "";
			}
		}

		/// <summary>
		/// Saves the settings to disk.
		/// </summary>
		/// <param name="settings">The settings object to save</param>
		/// <returns>True, if the settings were saved successfully, false otherwise.</returns>
		public static bool SaveSettings()
		{
			bool success;
			string settings_json = JsonConvert.SerializeObject(UserSettings, Formatting.Indented);
			//Overwrite existing settings file.
			try {
				using (var writer = new StreamWriter(SettingsPath))
				{
					writer.Write(settings_json);
					success = true;
				}
			} 
			catch (Exception)
			{
				success = false;
			}
			return success;
		}

		/// <summary>
		/// To be called when some settings changed. Only calls SaveSettings again
		/// </summary>
		/// <returns>True, if the settings could be dumped, else otherwise.</returns>
		public static bool SettingsChanged()
		{
			return SaveSettings();
		}
		#endregion

		public static TrySetResult TrySet(string name, string value)
		{
			//Instead of doing something fancy, this is just one giant switch-case for handling
			//individual settings. Ideally, I could associate a property to assign to straight from
			//its name, but then I'd also have to consider the types they could be set with
			//and that'd be too much.
			var qualifiedArg = settingsAliases.FindAlias(name);
			if (qualifiedArg == null)
				return TrySetResult.SettingNotFound;

			var result = setters[qualifiedArg](value);
			if(result == TrySetResult.Success)
			{
				SettingsChanged();
            }
			return result;
		}

		public static string TryGet(string name)
		{
			//Designed to be used with a "get" user command. The program should have other ways
			//of accessing members or member representation of strings.
			var qualifiedArg = settingsAliases.FindAlias(name);
			return GetValue(qualifiedArg)?.ToString();
		}

		private static TrySetResult SetPath(string value)
		{
			//Basic Path checking.
			if (value == null) return TrySetResult.NoValueSpecified;

			if (IsValidMediaPlayerPath(value))
			{
				UserSettings.media_player_path = value;
				MediaPlayerInterface.MediaPlayerPath = value;
				return TrySetResult.Success;
			}
			return TrySetResult.FileNotFound;
		}

		private static bool IsValidMediaPlayerPath(string path)
		{
			return File.Exists(path) && Path.GetExtension(path).ToLower() == ".exe";
		}

		private static TrySetResult SetDefQuality(string value)
		{
			if (value == null) return TrySetResult.NoValueSpecified;

			var quality = GetStreamMain.qualityAssocs.FirstOrDefault(e =>
				Util.GetQualityAlias(e, value)).Key;
			if (quality == null) return TrySetResult.InvalidValue;

			UserSettings.default_quality = quality;
			return TrySetResult.Success;
		}

		private static TrySetResult SetCB(string value)
		{
			Action<bool> set = b => UserSettings.copyToClipBoardMode = b;
			Func<bool> get = () => UserSettings.copyToClipBoardMode;
			return SetBoolProp(value, set, get);
		}

		private static TrySetResult SetMP(string value)
		{
			Action<bool> set = b => UserSettings.openMediaPlayerMode = b;
			Func<bool> get = () => UserSettings.openMediaPlayerMode;
			return SetBoolProp(value, set, get);
		}

		private static TrySetResult SetMonitor(string value)
		{
			if (value == null) return TrySetResult.NoValueSpecified;

			int monitor;
			if (int.TryParse(value, out monitor))
			{
				if (1 <= monitor && monitor <= Util.maxMonitorCount)
				{
					UserSettings.launchOnMonitor = monitor;
					return TrySetResult.Success;
				}
				return TrySetResult.InvalidValue;
			}
			return TrySetResult.InvalidValueType;
		}

		private static TrySetResult SetFullscreen(string value)
		{
			Action<bool> set = b => UserSettings.launchFullscreen = b;
			Func<bool> get = () => UserSettings.launchFullscreen;
			return SetBoolProp(value, set, get);
		}

		private static TrySetResult SetExitMPOnQuit(string value)
		{
			Action<bool> set = b => UserSettings.exitMPOnQuit = b;
			Func<bool> get = () => UserSettings.launchFullscreen;
			return SetBoolProp(value, set, get);
		}

		//Two parameters, one to a delegate that sets the property to an absolute value,
		//one that flips it.
		private static TrySetResult SetBoolProp(string value, Action<bool> set, Func<bool> get)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				set(true);
				return TrySetResult.Success;
			}
			if (toggleAssocs.Contains(value))
			{
				set(get() ^ true);
				return TrySetResult.Success;
			}

			bool setTo;
			if (boolAssocs.TryFindAlias(value, out setTo))
			{
				set(setTo);
				return TrySetResult.Success;
			}
			return TrySetResult.InvalidValue;
		}

		internal static IDictionary<string, object> GetAllSettings()
		{
			var dict = new Dictionary<string, object>();
			foreach (var key in settingsAliases.Keys)
			{
				dict.Add(key, GetValue(key));
			}
			return dict;
		}

		private static object GetValue(string setting)
		{
			switch (setting)
			{
				case "path": return UserSettings.media_player_path;
				case "default_quality": return UserSettings.default_quality;
				case "clipboard": return UserSettings.copyToClipBoardMode;
				case "mediaplayer": return UserSettings.openMediaPlayerMode;
				case "monitor": return UserSettings.launchOnMonitor;
				case "fullscreen": return UserSettings.launchFullscreen;
				case "exitmponquit": return UserSettings.exitMPOnQuit;
				default: return null;
			}
		}

		internal static string GetSettingDescription(string setting)
		{
			var qualifiedArg = settingsAliases.FindAlias(setting);
			if (qualifiedArg == null) return string.Empty;

			return settingsDescriptions[qualifiedArg];
		}

		internal static string GetSettingName(string setting)
		{
			return settingsAliases.FindAlias(setting);
		}
	}
}
