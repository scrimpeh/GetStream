using System;
using System.Linq;

using GetStream.Config;

using static GetStream.GetStreamMain;

namespace GetStream.MainFunctions
{
	internal static class HistoryFunctions
	{
		internal static void ClearHistory(string[] args)
		{
			history.Clear();
			SettingsManager.SettingsChanged();
		}

		internal static void AddToHistory(string channel)
		{
			var end = Settings.maxhist - 1;
			if (history.Contains(channel))  // Swap element with the to the front.
			{
				history.Remove(channel);
				history.Insert(0, channel);
			}
			else                            //M ust insert a new element
			{
				if (history.Count > end)
				{
					history.RemoveAt(end);  // Remove final element
				}
				history.Insert(0, channel);
			}
			SettingsManager.SettingsChanged();
		}

		internal static void RemoveFromHistory(string channel)
		{
			history.Remove(channel);
			SettingsManager.SettingsChanged();
		}

		internal static void DisplayHistory(string[] args)
		{
			for (var i = 0; i < history.Count; i++)
			{
				Console.WriteLine(string.Format(Strings.hist_identifier, i + 1, history[i]));
			}
			if (history.Count == 0)
			{
				Console.WriteLine(Strings.no_channels_in_hist_msg);
			}
		}

		internal static void DisplayHistoryStartup()
		{
			if (history.Any())
			{
				Console.WriteLine();
				Console.WriteLine(Strings.hist_msg);
				DisplayHistory(null);   //May need to add dummy array here just in case.
				Console.WriteLine();
			}
		}
	}
}
