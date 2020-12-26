using PingClientBase;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TCPingClient
{
	public class TCPingClient : IPingClient
	{
		public string ProtocolName { get; } = @"TCP";

		public TimeSpan Timeout { get; set; }

		public string Arguments { get; set; }

		public IPEndPoint EndPoint { get; set; }

		public ValueTask<PingResult> PingAsync(CancellationToken token)
		{
			throw new NotImplementedException();
		}

		public PingResult Ping()
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public ValueTask DisposeAsync()
		{
			throw new NotImplementedException();
		}
	}
}
