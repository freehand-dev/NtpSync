using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NtpClient
{
	public class NtpClient
	{
		private const int NTP_PORT = 123;
		private const int DEFAULT_TIMEOUT = 500;

		private readonly IPEndPoint _endpoint;
		public int Timeout { get; set; }

		public IPAddress Peer { get => _endpoint.Address; }

		public int Port { get => _endpoint.Port; }

		public NtpClient(IPEndPoint endpoint)
		{
			this.Timeout = NtpClient.DEFAULT_TIMEOUT;
			this._endpoint = endpoint;
		}

		public NtpClient(string ntpServer = "pool.ntp.org", int port = NTP_PORT): 
			this(new IPEndPoint(
				Dns.GetHostAddresses(ntpServer)[0], port))
		{

		}

		public NtpClient(IPAddress ntpServer, int port = NTP_PORT): 
			this(new IPEndPoint(ntpServer, port))
		{

		}

		public async Task<NtpPacket> QueryAsync()
		{
			return await NtpClient.QueryAsync(this.Peer.ToString(), this.Port, this.Timeout);
		}

		public async Task<TimeSpan> GetOffsetAsync()
		{
			var x = await QueryAsync();
			return x.Offset;
		}


		static public async Task<NtpPacket> QueryAsync(string ntpServer, int port = NtpClient.NTP_PORT,  int timeout = NtpClient.DEFAULT_TIMEOUT)
		{
			IPEndPoint endpoint = new IPEndPoint(
			   Dns.GetHostAddresses(ntpServer)[0], port);

			NtpPacket ntpPacket = default;
			NtpPacket requestPacket = new NtpPacket();
			using (UdpClient udpClient = new UdpClient())
			{
				udpClient.Connect(endpoint);
				await udpClient.SendAsync(requestPacket.Bytes, requestPacket.Size).ConfigureAwait(false);
				var response = await udpClient.ReceiveAsync(timeout, new CancellationToken());
				if (response.Buffer != null)
					ntpPacket = new NtpPacket(response.Buffer)
					{
						DestinationTimestamp = DateTime.UtcNow,
						Server = endpoint
					};
			}
			return ntpPacket;
		}

	}
}
