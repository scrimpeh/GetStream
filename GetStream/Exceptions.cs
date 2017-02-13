using System;

namespace GetStream
{
	/// <summary>
	/// Exception to use if the URL could not be found, either because of an Invalid Domain Name
	/// or an invalid http call.
	/// </summary>
	[Serializable]
	public class URINotFoundException : Exception
	{
		public URINotFoundException() : base() { }

		public URINotFoundException(string message) : base(message) { }

		public URINotFoundException(string message, Exception inner) : base(message, inner) { }
	}

	/// <summary>
	/// Exception to use if Twitch returns a 404. We couldn't find the channel.
	/// </summary>
	[Serializable]
	public class ChannelNotFoundException : Exception
	{
		public ChannelNotFoundException() : base() { }

		public ChannelNotFoundException(string message) : base(message) { }

		public ChannelNotFoundException(string message, Exception inner) : base(message, inner) { }
	}

	/// <summary>
	/// Exception to use if Twitch returns a 404 on a VOD call. We couldn't find the VOD.
	/// </summary>
	[Serializable]
	public class VODNotFoundException : Exception
	{
		public VODNotFoundException() : base() { }

		public VODNotFoundException(string message) : base(message) { }

		public VODNotFoundException(string message, Exception inner) : base(message, inner) { }
	}

	/// <summary>
	/// Exception to use if we couldn't parse a channel's playlist. This generally indicates
	/// that the channel is offline.
	/// </summary>
	[Serializable]
	public class ChannelOfflineException : Exception 
	{
		string channel;

		public ChannelOfflineException() : base("Channel offline or unparseable.") 
		{ 
			channel = "Unknown"; 
		}

		public ChannelOfflineException(string channelname) : base("Channel " + channelname + " offline or unparseable")
		{
			channel = channelname;
		}
	}
}

