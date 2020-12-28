using Microsoft.Extensions.Logging;
using PingClientBase;
using PingClientBase.Enums;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TCPingClient
{
	public sealed class TCPingClient : IPingClient
	{
		private readonly ILogger _logger;
		private const string LoggerHeader = @"[TCP]";

		public string ProtocolName { get; } = @"TCP";

		public TimeSpan Timeout { get; set; }

		public string? Arguments { get; set; }

		public IPEndPoint? EndPoint { get; set; }

		public TCPingClient(ILogger<TCPingClient> logger)
		{
			_logger = logger;
		}

		public async ValueTask<PingResult> PingAsync(CancellationToken token)
		{
			try
			{
				token.ThrowIfCancellationRequested();
				CheckAddress();
				CheckPort();

				using var tcp = new TcpClient(EndPoint!.AddressFamily);

				var sw = Stopwatch.StartNew();
				var task = tcp.ConnectAsync(EndPoint.Address, EndPoint.Port, token).AsTask();
				var timeoutTask = Task.Delay(Timeout, token);
				var resTask = await Task.WhenAny(task, timeoutTask);
				sw.Stop();

				if (resTask.IsFaulted && resTask.Exception?.InnerException is not null)
				{
					throw resTask.Exception.InnerException;
				}

				if (resTask != task)
				{
					if (token.IsCancellationRequested)
					{
						throw new OperationCanceledException();
					}
					throw new TimeoutException();
				}

				if (tcp.Connected)
				{
					return new PingResult
					{
						Latency = sw.ElapsedMilliseconds,
						Status = PingStatus.Success,
						Info = $@"Success: {tcp.Client.LocalEndPoint} => {tcp.Client.RemoteEndPoint}"
					};
				}
				throw new TCPingException(-1.0, PingStatus.Failed, @"Connect failed");
			}
			catch (TCPingException ex)
			{
				return ex.Result;
			}
			catch (OperationCanceledException ex)
			{
				return new PingResult
				{
					Latency = -1.0,
					Status = PingStatus.Unknown,
					Info = ex.Message
				};
			}
			catch (TimeoutException ex)
			{
				return new PingResult
				{
					Latency = Timeout.TotalMilliseconds,
					Status = PingStatus.TimedOut,
					Info = ex.Message
				};
			}
			catch (Exception ex)
			{
				if (ex is not SocketException)
				{
					_logger.LogError(ex, @"{0} ", LoggerHeader);
				}
				return new PingResult
				{
					Latency = -1.0,
					Status = PingStatus.Failed,
					Info = ex.Message
				};
			}
		}

		private void CheckAddress()
		{
			if (EndPoint?.Address is not null)
			{
				return;
			}
			throw new TCPingException(-1.0, PingStatus.DestinationError, @"Wrong ip address");
		}

		private void CheckPort()
		{
			if (EndPoint?.Port is > IPEndPoint.MinPort and <= IPEndPoint.MaxPort)
			{
				return;
			}
			throw new TCPingException(-1.0, PingStatus.PortError, @"Wrong port");
		}

		public void Dispose()
		{

		}

		public ValueTask DisposeAsync()
		{
			return default;
		}
	}
}
