using PingClientBase;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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

		public async ValueTask<PingResult> PingAsync(CancellationToken token)
		{
			try
			{
				token.ThrowIfCancellationRequested();

				if (EndPoint?.Address == null)
				{
					return new PingResult
					{
						Latency = -1.0,
						Status = IPStatus.BadDestination,
						Info = @"Wrong ip address"
					};
				}

				if (EndPoint.Port <= IPEndPoint.MinPort || EndPoint.Port > IPEndPoint.MaxPort)
				{
					return new PingResult
					{
						Latency = -1.0,
						Status = IPStatus.DestinationPortUnreachable,
						Info = @"Wrong port"
					};
				}

				using var tcp = new TcpClient(EndPoint.AddressFamily);

				var sw = new Stopwatch();

				sw.Start();

				var task = tcp.ConnectAsync(EndPoint.Address, EndPoint.Port);

				var resTask = await Task.WhenAny(task, Task.Delay(Timeout, token));

				sw.Stop();

				if (resTask.IsFaulted && resTask.Exception?.InnerException != null)
				{
					throw resTask.Exception?.InnerException;
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
						Status = IPStatus.Success,
						Info = $@"Success: {tcp.Client.LocalEndPoint} => {tcp.Client.RemoteEndPoint}"
					};
				}
				throw new Exception(@"Failed");
			}
			catch (OperationCanceledException)
			{
				return new PingResult
				{
					Latency = -1.0,
					Status = IPStatus.Unknown,
					Info = @"Task was canceled"
				};
			}
			catch (TimeoutException)
			{
				return new PingResult
				{
					Latency = Timeout.TotalMilliseconds,
					Status = IPStatus.TimedOut,
					Info = @"TimedOut"
				};
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return new PingResult
				{
					Latency = -1.0,
					Status = IPStatus.Unknown,
					Info = ex.Message
				};
			}
		}

		public ValueTask DisposeAsync()
		{
			return default;
		}
	}
}