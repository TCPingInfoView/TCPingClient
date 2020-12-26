using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PingClientBase;
using PingClientBase.Enums;
using System;
using System.Net;
using System.Threading.Tasks;

namespace UnitTest
{
	[TestClass]
	public class UnitTest
	{
		private static IPingClient NewTCPingClient => new TCPingClient.TCPingClient(NullLogger<TCPingClient.TCPingClient>.Instance);

		[TestMethod]
		public void TestProtocolName()
		{
			using var client = NewTCPingClient;

			Assert.AreEqual(@"TCP", client.ProtocolName);
		}

		[TestMethod]
		[DataRow(@"127.0.0.1", 135)]
		[DataRow(@"[::1]", 135)]
		public async Task TestSuccessAsync(string ip, int port)
		{
			await using var client = NewTCPingClient;
			client.Timeout = TimeSpan.FromSeconds(3);
			client.EndPoint = IPEndPoint.Parse($@"{ip}:{port}");

			var res0 = await client.PingAsync(default);
			Console.WriteLine(res0);
			Assert.AreEqual(PingStatus.Success, res0.Status);
			Assert.IsTrue(res0.Latency < client.Timeout.TotalSeconds);

			var res1 = client.Ping();
			Console.WriteLine(res1);
			Assert.AreEqual(PingStatus.Success, res1.Status);
			Assert.IsTrue(res1.Latency < client.Timeout.TotalSeconds);
		}
	}
}
