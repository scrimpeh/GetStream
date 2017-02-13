using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;

using GetStream.Config;

namespace GetStream
{
	static class MediaPlayerInterface
	{
		//Todo: Avoid default, instead make main program set this
		//Add methods to check for and verify this path is correct.

		//Media Player Launch Options.
		//These are to be moved to the settings dialog and are therefore obsolete here
		//since I want to keep everything in one spot.
		static Settings settings;
		
		//Constants
		const int WM_COPYDATA = 0x004A;
		const ulong CMD_OPENFILE = 0xA0000000;
		const ulong CMD_CLOSEAPP = 0xA0004006;

		static bool mediaPlayerOpen = false;
		static IntPtr mediaPlayerHandle = IntPtr.Zero;
		static IntPtr hwnd = Process.GetCurrentProcess().Handle;
		static Process mediaPlayerProcess = null;

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		public static bool MediaPlayerOpen 
		{ 
			get { return mediaPlayerOpen; } 
		}

		public static string MediaPlayerPath { get; set; }

		public static Settings Settings
		{
			set { settings = value; }
		}

		public static void OpenMediaPlayer(string mediaURL, MediaPlayerArgs args)
		{
			//There seems to be a pretty nasty bug that sometimes locks up the media player and the 
			//console app when trying to parse VODs. It seems that either MPC or the 
			//Console app get too busy while trying to interact, leading to delays and sometimes
			//errors. TODO: Fix.
			if (mediaPlayerProcess != null && mediaPlayerOpen)
			{
				IntPtr curProcHandle = mediaPlayerProcess.MainWindowHandle;
				if (curProcHandle == IntPtr.Zero)
				{
					Console.WriteLine(Strings.media_player_not_ready_msg);
					return;
				}
				unsafe
				{
					fixed (void* p = mediaURL)
					{
						var bufsize = (mediaURL.Length + 1) * 2;
						var cds = new COPYDATASTRUCT() { dwData = CMD_OPENFILE, cbData = bufsize, lpData = p };
						var cdsPtr = new IntPtr(&cds);
						SendMessage(curProcHandle, WM_COPYDATA, hwnd, cdsPtr);
					}
				}
			}
			else
			{
				//Generate new Media Player process and launch it.
				var processStartInfo = new ProcessStartInfo();
				processStartInfo.Arguments = CreateLaunchArgs(mediaURL, args);
				processStartInfo.FileName = MediaPlayerPath;
				var mediaPlayer = new Process();
				mediaPlayer.StartInfo = processStartInfo;
				mediaPlayer.EnableRaisingEvents = true;
				mediaPlayer.Exited += OnMediaPlayerExit;
				mediaPlayerOpen = true;
				mediaPlayerProcess = mediaPlayer;   //Hacky solution, but the best I can do
													//without initializing a message loop somewhere.
													//The user should not be able to 
													//"change the channel" quick enough to 
													//throw this out of order.
													//Still, the eventuality needs to be considered
													//that it does happen.
				mediaPlayer.Start();
			}
		}

		//Send a command to the mediaplayer.
		public static void CloseMediaPlayer()
		{
			if (mediaPlayerProcess != null && mediaPlayerOpen)
			{
				IntPtr curProcHandle = mediaPlayerProcess.MainWindowHandle;
				if (curProcHandle == IntPtr.Zero)
				{
					Console.WriteLine(Strings.media_player_not_ready_msg);
					return;
				}
				unsafe 
				{
					var cds = new COPYDATASTRUCT() { dwData = CMD_CLOSEAPP, cbData = 0, lpData = (void*)0 };
					var cdsPtr = new IntPtr(&cds);
					SendMessage(curProcHandle, WM_COPYDATA, hwnd, cdsPtr);
				}
			}
		}

		private static void OnMediaPlayerExit(object o, EventArgs e)
		{
			//Should probably log this somewhere in case of unexpected exit.
			mediaPlayerOpen = false;
			mediaPlayerProcess = null;
		}

		private static string CreateLaunchArgs(string path, MediaPlayerArgs args)
		{
			var sb = new StringBuilder(path);
			sb.Append(" /new ");
			sb.AppendFormat("/slave {0} ", hwnd);
			if (args.FullScreen) sb.Append("/fullscreen ");
			sb.AppendFormat("/monitor {0} ", args.Monitor);

			if(args.SetStartPos)
			{
				sb.AppendFormat(
					"/startpos {0:D2}:{1:D2}:{2:D2} ",
					args.StartTime.Hours,
					args.StartTime.Minutes,
					args.StartTime.Seconds );
			}

			sb.Append("/nofocus");
			return sb.ToString();
		}

		public static MediaPlayerArgs GetDefaultArgs()
		{
			return new MediaPlayerArgs()
			{
				FullScreen = settings.launchFullscreen,
				Monitor = settings.launchOnMonitor,
            };
		}
	}

	/// <summary>
	/// A basic data object containing launch arguments for the media player.
	/// The sole exception is the path, since that is supposed to be 
	/// a permanent setting.
	/// </summary>
	internal class MediaPlayerArgs	//Should be a struct, but that would allow inconsistent states
	{								//because of no auto-initialization.
		private int _monitor = 1;

		/// <summary>
		/// Launch the media player in fullscreen mode
		/// </summary>
		public bool FullScreen { get; set; }

		/// <summary>
		/// Launch the Media Player in which monitor.
		/// </summary>
		public int Monitor
		{
			get { return _monitor; }
			set { if (0 < value && value <= Screen.AllScreens.Length) _monitor = value; }
		}

		/// <summary>
		/// Start the the media player at the time indicated by this time span.
		/// </summary>
		public TimeSpan StartTime { get; set; } = TimeSpan.Zero;

		/// <summary>
		/// If false, the media file starts from the beginning. This is implicitely true
		/// on stream URLs, if true, confer StartTime for the starting position.
		/// </summary>
		public bool SetStartPos 
		{ 
			get { return StartTime > TimeSpan.Zero; } 
		}

		/// <summary>
		/// The URL of the media to load.
		/// </summary>
		public string MediaURL { get; set; } = string.Empty;

		internal MediaPlayerArgs() { }
	}
}
