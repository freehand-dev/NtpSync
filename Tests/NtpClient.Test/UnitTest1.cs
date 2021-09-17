using System;
using Xunit;
using Xunit.Abstractions;

namespace NtpClient.Test
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _output;

        public UnitTest1(ITestOutputHelper output)
        {
            this._output = output;
        }

        [Theory]
        [InlineData("ntp.time.in.ua")]
        [InlineData("ntp2.time.in.ua")]
        [InlineData("ntp3.time.in.ua")]
        [Trait("Category", "NtpClient")]
        public async void NtpQueryAsync(string peer)
        {
            NtpClient ntpClient = new NtpClient(peer);
            NtpPacket ntpPacket = await ntpClient.QueryAsync();
            _output.WriteLine(ntpPacket.ToString());
        }

    }
}
