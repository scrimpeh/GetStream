using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GetStream.Config
{
	//Since we already use the Json serializer, the plan is to keep 
	//all the persistent user data in a single class that can be serialized to and from
	//disk.
	//This class would at least contain a history, along with a path to the user's media player
	//IDEA: Perhaps, to compartmentalize the program better, I should split up the functionality
	//into a record Type "Settings" object that stores nothing except the raw settings,
	//and a settings manager that handles added functionality, along with reading
	//settings from JSon and so on.
	public class Settings
	{
		public const int maxhist = 10;

		public Settings()
		{
			//Keeping this around in case we need to do
			//some initializing in this class.
		}

		public string _comment { get; } = "GetStream User Settings";

		[UserSettable]
		public string media_player_path { get; set; } = "";

		[JsonIgnore]
		public bool media_player_path_known 
		{ 
			get { return !string.IsNullOrWhiteSpace(media_player_path); } 
		}

		[UserSettable]
		public string default_quality { get; set; } = "source";

		public IList<string> channel_history { get; set; } = new List<string>();

		[UserSettable]
		public bool copyToClipBoardMode { get; set; } = false;

		// Just a shadow for copyToClipboardMode
		[UserSettable]
		public bool openMediaPlayerMode
		{
			get { return !copyToClipBoardMode; }
			set { copyToClipBoardMode = !value; }
		}

		[UserSettable]
		public int launchOnMonitor { get; set; } = 1;

		[UserSettable]
		public bool launchFullscreen { get; set; } = false;

		[UserSettable]
		public bool exitMPOnQuit { get; set; } = false;

		// This attribute signifies that a property is user settable.
		// However, until I invoke it at some point, this isn't going to do much
		// and the actual code rides on dictioanries.
		[AttributeUsage(AttributeTargets.Property)]
		private class UserSettableAttribute : Attribute
		{
			public UserSettableAttribute() { }
		}

		// Ditto
		[AttributeUsage(AttributeTargets.Property)]
		private class UserGettableAttribute : Attribute
		{
			public UserGettableAttribute() { }
		}
	}
}
