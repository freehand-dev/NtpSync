using System;
using System.Collections.Generic;
using System.Text;
using static NtpClient.NtpPacket;

namespace NtpClient.Extensions
{
    public static class NtpStratumExtensions
    {
        public static int ToReliable(this NtpStratum stratum)
        {
            switch (stratum)
            {
                case NtpStratum.PrimaryReference:
                    return 0;
                case NtpStratum.SecondaryReference:
                    return 1;
                case NtpStratum.Reserved:
                    return 2;
                case NtpStratum.Unspecified:
                    return 3;
                default:
                    return 4;
            }
        }
    }
}
