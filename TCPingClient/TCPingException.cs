using PingClientBase;
using PingClientBase.Enums;
using System;

namespace TCPingClient
{
	public class TCPingException : Exception
	{
		public PingResult Result { get; }

		public TCPingException(double latency, PingStatus status, string? info)
		{
			Result = new PingResult
			{
				Latency = latency,
				Status = status,
				Info = info
			};
		}
	}
}
