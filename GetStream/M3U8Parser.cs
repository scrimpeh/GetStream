using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections;

namespace GetStream
{
	//Basic class containing a data definition file for M3U8
	//along with a parser format to get it from string.
	public static class M3U8Parser
	{
		//Basic helper dictionary to associate Media Types to their appropriate enum vavlues.
		//We could try parsing the string as well, but that'd be less convenient than just
		//using a dictionary.
		private static IDictionary<string, MediaType> mediaTypeNames = new Dictionary<string, MediaType>()
		{
			{ "VIDEO", MediaType.Video },
			{ "AUDIO", MediaType.Audio }
		};	

		/// <summary>
		/// Returns an M3U8 object representing an M3U playlist from a raw text string.
		/// </summary>
		/// <param name="input">The raw M3U8 in text form.</param>
		/// <returns>The M3U8 containing the media links. Null, if there were errors while parsing</returns>
		public static M3U8 ParseM3U8(string input)
		{
			//M3U8 files are essentially just comments and video links. We can try to ignore missing comments,
			//and video links too, if needed.
			M3U8 m3u8 = new M3U8();
			var lines = input.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

			Hashtable media = null;           
			Hashtable stream_inf = null;		
			string currentQuality = string.Empty;
			foreach(var line in lines)
			{
				if(line.StartsWith("#"))
				{
					var metadata = line.Substring(1).Split(':');
					var parameters = metadata.Length > 1 ? SplitM3U8Line(metadata[1]) : new string[0];
					switch(metadata[0]) 
					{
						case "EXT3MU":				//header line
							break;
						case "EXT-X-TWITCH-INFO":   //global m3u metadata.
							SetM3U8Header(m3u8, parameters);
                            break;
						case "EXT-X-MEDIA":         //meta data for one playlist link.
							media = new Hashtable();
							SetMediaInfo(media, parameters, ref currentQuality);
							break;
						case "EXT-X-STREAM-INF":    //stream data for one playlist link.
							stream_inf = new Hashtable();
							SetStreamInfo(stream_inf, parameters);
							break;
					}
				}
				else
				{
					//we got a link. finish the tuple that we have collected.
					var curM3U8 = new M3U8Tuple(line, media, stream_inf);
					m3u8.MediaLinks.Add(currentQuality, curM3U8);
				}
			}	
			return m3u8;
		}

#region setM3U8Details
		private static void SetM3U8Header(M3U8 m3u8, IEnumerable<string> parameters)
		{
			foreach(var p in parameters)
			{
				var key_value = p.Split('=');
				var value = key_value[1].Trim('\"');
				switch(key_value[0])
				{
					case "STREAM-TIME":
						m3u8.Metadata[key_value[0]] = double.Parse(value);
						break;
					case "NODE":			//Generic string type arguments
					case "MANIFEST-NODE":
					case "SERVER-TIME":
					case "USER-IP":
					case "CLUSTER":
					case "ORIGIN":          //For vods only
					case "MANIFEST-CLUSTER":
					case "REGION":
					default:
						m3u8.Metadata[key_value[0]] = value;
						break;
				}
			}
		}

		private static void SetMediaInfo(Hashtable mi, IEnumerable<string> parameters, ref string urlQuality)
		{
			foreach(var p in parameters)
			{
				var key_value = p.Split('=');
				switch (key_value[0])
				{
					case "TYPE":
						mi[key_value[0]] = mediaTypeNames[key_value[1]];
						break;
					case "GROUP-ID":
						mi[key_value[0]] = key_value[1].Trim('\"');
						break;
					case "NAME":    //This one is responsible for writing the actual quality option in the dictionary.
						var quality = key_value[1].Trim('\"');
						mi[key_value[0]] = quality;
						urlQuality = quality.ToLower().Replace(" ", null);
						break;
					case "AUTOSELECT":
					case "DEFAULT":
						mi[key_value[0]] = key_value[1] == "YES";
						break;
					default:
						break;
				}
			}
		}

		private static void SetStreamInfo(Hashtable si, IEnumerable<string> parameters)
		{
			foreach(var p in parameters)
			{
				var key_value = p.Split('=');
				switch(key_value[0])
				{
					case "PROGRAM-ID":
					case "BANDWIDTH":
						si[key_value[0]] = int.Parse(key_value[1]);
						break;
					case "RESOLUTION":
						var resolutions = key_value[1].Trim('\"').Split('x').Select(int.Parse).ToArray();
						si["X-RESOLUTION"] = resolutions[0];
						si["Y-RESOLUTION"] = resolutions[1];
						break;
					case "CODECS":
						si[key_value[0]] = key_value[1].Trim('\"').Split(',');
						break;
					case "VIDEO":
						si[key_value[0]] = key_value[1];
						break;
					default:
						break;
				}
			}
		}
#endregion

		//Helper function to parse an M3U8 Line
		//You would think string.Split(',') would be sufficient, but no,
		//because codecs are stated as "videocodec,audiocodec", breaking string.split.
		//So because of this we have to hack together our own function. 
		//yes, you could do this with regexes, but I don't want to. gj m3u
		private static IEnumerable<string> SplitM3U8Line(string input)
		{
			var from = -1;
			var inQuote = false;
			for(int i = 0; i < input.Length; ++i)
			{
				switch(input[i])
				{
					case ',':
						if(!inQuote)
						{
							++from;
							yield return input.Substring(from, (i - from));
							from = i;
						}
						break;
					case '\"':
						inQuote ^= true;
						break;
				}
			}
		}
	}

	public class M3U8
	{
		//Most of these properties are superfluous and unneeded. I may cut parsing to them if it's not required.
		/*public string Node { get; set; }
		public string ManifestNode { get; set; }
		public double StreamTime { get; set; }
		public string UserIP { get; set; }
		public string Cluster { get; set; }*/
		public Hashtable Metadata { get; set; } = new Hashtable();
        public IDictionary<string, M3U8Tuple> MediaLinks { get; set; } = new Dictionary<string, M3U8Tuple>();

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach(var tuple in MediaLinks.Values)
			{
				sb.AppendLine(tuple.ToString());
			}
			return sb.ToString();
		}
	}

	//Each M3U8 Tuple is at its core a tuple of all the relevant data per link.
	public class M3U8Tuple
	{
		/// <summary>
		/// The http link to the media.
		/// </summary>
		public string MediaLink { get; private set; }

		public Hashtable MediaInfo { get; private set; }	//Metadata for the stream
		public Hashtable StreamInfo { get; private set; }	//link.

		public M3U8Tuple(string media_link, Hashtable media_info, Hashtable stream_info)
		{
			MediaLink = media_link;
			MediaInfo = media_info;
			StreamInfo = stream_info;
		}

		public override string ToString() =>
			$"[{Name}:" +
			$" {XResolution}x{YResolution}" +
			$" @ {Bandwidth / 1000}K -" +
			$" {MediaLink.Substring(0,64)}...]";

		public string Name => (string)MediaInfo["NAME"];
		public int XResolution => GetValueField<int>("X-RESOLUTION");
		public int YResolution => GetValueField<int>("Y-RESOLUTION");
		public int Bandwidth => GetValueField<int>("BANDWIDTH");

		private T GetValueField<T>(string key) where T : struct
		{
			var result = StreamInfo[key];
			return result != null ? (T)result : default(T);
		}
    }

	public enum MediaType 
	{
		Video,
		Audio
		//there may be others, but only video is important for us.
	};
}
