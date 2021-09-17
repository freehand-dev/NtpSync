using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NtpClient
{
    internal static class UdpClientExtensions
    {
        public static async Task<UdpReceiveResult> ReceiveAsync(this UdpClient udpClient, int timeout, CancellationToken token)
        {
            var connectTask = udpClient.ReceiveAsync();
            var timeoutTask = Task.Delay(timeout, token);

            await Task.WhenAny(connectTask, timeoutTask);

            if (connectTask.IsCompleted)
                return connectTask.Result;

            if (timeoutTask.IsCompleted)
                throw new TimeoutException();

            return new UdpReceiveResult();
        }
    }
}
