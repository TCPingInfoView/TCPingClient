using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PingClientBase;
using PingClientBase.Enums;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTest
{
	[TestClass]
	public class UnitTest
	{
		private static TestContext _testContext = null!;
		private static IPingClient NewTCPingClient => new TCPingClient.TCPingClient(NullLogger<TCPingClient.TCPingClient>.Instance);
		private static TcpListener _tcpListenerV4 = null!;
		private static TcpListener _tcpListenerV6 = null!;
		private const ushort SuccessPort = 12345;

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			_testContext = context;

			_tcpListenerV4 = new TcpListener(IPAddress.Loopback, SuccessPort);
			_tcpListenerV4.Start();

			_tcpListenerV6 = new TcpListener(IPAddress.IPv6Loopback, SuccessPort);
			_tcpListenerV6.Start();
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			_tcpListenerV4.Stop();
			_tcpListenerV6.Stop();
		}

		private static int GetFreePort(IPAddress ip)
		{
			var tcpListener = new TcpListener(ip, 0);
			try
			{
				tcpListener.Start();
				return ((IPEndPoint)tcpListener.LocalEndpoint).Port;
			}
			finally
			{
				tcpListener.Stop();
			}
		}

		[TestMethod]
		public void TestProtocolName()
		{
			using var client = NewTCPingClient;

			Assert.AreEqual(@"TCP", client.ProtocolName);
		}

		[TestMethod]
		[DataRow(@"127.0.0.1", SuccessPort)]
		[DataRow(@"[::1]", SuccessPort)]
		public async Task TestSuccessAsync(string ip, int port)
		{
			await using var client = NewTCPingClient;
			client.Timeout = TimeSpan.FromSeconds(3);
			client.EndPoint = IPEndPoint.Parse($@"{ip}:{port}");

			var res = await client.PingAsync(default);
			_testContext.WriteLine($@"{res}");
			Assert.AreEqual(PingStatus.Success, res.Status);
			Assert.IsTrue(res.Latency < client.Timeout.TotalMilliseconds);
		}

		[TestMethod]
		[DataRow(@"127.0.0.1")]
		[DataRow(@"[::1]")]
		public async Task TestFailedAsync(string ip)
		{
			var port = GetFreePort(IPAddress.Parse(ip));

			await using var client = NewTCPingClient;
			client.Timeout = TimeSpan.FromSeconds(3);
			client.EndPoint = IPEndPoint.Parse($@"{ip}:{port}");

			var res = await client.PingAsync(default);
			Assert.AreEqual(PingStatus.Failed, res.Status);
			Assert.AreEqual(-1.0, res.Latency);
		}

		[TestMethod]
		[DataRow(@"127.0.0.1", 0)]
		[DataRow(@"[::1]", 0)]
		public async Task TestPortErrorAsync(string ip, int port)
		{
			await using var client = NewTCPingClient;
			client.EndPoint = IPEndPoint.Parse($@"{ip}:{port}");

			var res0 = await client.PingAsync(default);
			Assert.AreEqual(PingStatus.PortError, res0.Status);
			Assert.AreEqual(-1.0, res0.Latency);
		}

		[TestMethod]
		public async Task TestDestinationErrorAsync()
		{
			await using var client = NewTCPingClient;

			var res = await client.PingAsync(default);
			Assert.AreEqual(PingStatus.DestinationError, res.Status);
			Assert.AreEqual(-1.0, res.Latency);
		}

		[TestMethod]
		[DataRow(@"1.0.0.1", 80)]
		[DataRow(@"1.0.0.1", 443)]
		public async Task TestCanceledAsync(string ip, int port)
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(0));

			await using var client = NewTCPingClient;
			client.Timeout = TimeSpan.FromSeconds(3);
			client.EndPoint = IPEndPoint.Parse($@"{ip}:{port}");

			var res = await client.PingAsync(cts.Token);
			Assert.AreEqual(PingStatus.Unknown, res.Status);
			Assert.AreEqual(-1.0, res.Latency);
			Assert.AreEqual(@"The operation was canceled.", res.Info);
		}

		[TestMethod]
		[DataRow(@"1.0.0.1", 80)]
		[DataRow(@"1.0.0.1", 443)]
		public async Task TestTimedOutAsync(string ip, int port)
		{
			var timeout = TimeSpan.FromMilliseconds(0);

			await using var client = NewTCPingClient;
			client.Timeout = timeout;
			client.EndPoint = IPEndPoint.Parse($@"{ip}:{port}");

			var res = await client.PingAsync(default);
			Assert.AreEqual(PingStatus.TimedOut, res.Status);
			Assert.AreEqual(timeout.TotalMilliseconds, res.Latency);
		}
	}
}
