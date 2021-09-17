using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NtpClient
{
    ///
    /// LeapIndicator - Warns of an impending leap second to be inserted/deleted in the last
    /// minute of the current day. (See the _LeapIndicator enum)
    /// 
    /// VersionNumber - Version number of the protocol (3 or 4).
    /// 
    /// Mode - Returns mode. (See the _Mode enum)
    /// 
    /// Stratum - Stratum of the clock. (See the _Stratum enum)
    /// 
    /// PollInterval - Maximum interval between successive messages
    /// 
    /// Precision - Precision of the clock
    /// 
    /// RootDelay - Round trip time to the primary reference source.
    /// 
    /// RootDispersion - Nominal error relative to the primary reference source.
    /// 
    /// ReferenceID - Reference identifier (either a 4 character string or an IP address).
    /// 
    /// ReferenceTimestamp - The time at which the clock was last set or corrected.
    /// 
    /// OriginateTimestamp - The time at which the request departed the client for the server.
    /// 
    /// ReceiveTimestamp - The time at which the request arrived at the server.
    /// 
    /// Transmit Timestamp - The time at which the reply departed the server for client.
    /// 
    /// RoundTripDelay - The time between the departure of request and arrival of reply.
    /// 
    /// LocalClockOffset - The offset of the local clock relative to the primary reference
    /// source.
    /// 
    /// Initialize - Sets up data structure and prepares for connection.
    /// 
    /// Connect - Connects to the time server and populates the data structure.
    ///	It can also update the system time.
    /// 
    /// IsResponseValid - Returns true if received data is valid and if comes from
    /// a NTP-compliant time server.
    /// 
    /// ToString - Returns a string representation of the object.
    /// 
    /// -----------------------------------------------------------------------------
    /// Structure of the standard NTP header (as described in RFC 2030)
    ///                       1                   2                   3
    ///   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |LI | VN  |Mode |    Stratum    |     Poll      |   Precision   |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                          Root Delay                           |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                       Root Dispersion                         |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                     Reference Identifier                      |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                   Reference Timestamp (64)                    |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                   Originate Timestamp (64)                    |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                    Receive Timestamp (64)                     |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                    Transmit Timestamp (64)                    |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                 Key Identifier (optional) (32)                |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                                                               |
    ///  |                 Message Digest (optional) (128)               |
    ///  |                                                               |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// 
    /// -----------------------------------------------------------------------------
    public class NtpPacket
    {
        public static readonly DateTime EpochTime = new DateTime(1970, 1, 1);
        public static readonly DateTime NtpEpochTime = new DateTime(1900, 1, 1); 

        private const int NTP_PACKET_SIZE = 48;

        public const int Length = NTP_PACKET_SIZE;

        public byte[] Bytes { get; private set; }

        public int Size { 
            get 
            {
                return Length;
            } 
        }

        public IPEndPoint Server { get; set; }

        /// <summary>
        /// NTP Mode field values
        /// </summary>
        public enum NtpMode
        {
            Unknown,            // 0, 6, 7 - Reserved
            SymmetricActive,    // 1 - Symmetric active
            SymmetricPassive,   // 2 - Symmetric pasive
            Client,             // 3 - Client
            Server,             // 4 - Server
            Broadcast           // 5 - Broadcast
        }

        /// <summary>
        /// NTP Leap indicator field values
        /// </summary>
        public enum NtpLeapIndicator
        {
            NoWarning,              // 0 - No warning
            LastMinuteHas61Seconds, // 1 - Last minute has 61 seconds
            LastMinuteHas59Seconds, // 2 - Last minute has 59 seconds
            AlarmCondition          // 3 - Alarm condition (clock not synchronized)
        }

        /// <summary>
        /// NTP Stratum field values
        /// </summary>
        public enum NtpStratum
        {
            Unspecified = 0,            // 0 - unspecified or unavailable
            PrimaryReference = 1,       // 1 - primary reference (e.g. radio-clock)
            SecondaryReference = 2,     // 2-15 - secondary reference (via NTP or SNTP)
            Reserved = 16               // 16-255 - reserved
        }

        /// <summary>
        /// Leap Indicator
        /// </summary>
        public NtpLeapIndicator LeapIndicator
        {
            get 
            { 
                return (NtpLeapIndicator)((Bytes[0] & 0xc0) >> 6); 
            }
        }

        /// <summary>
        ///  Version Number
        /// </summary>
        public int Version
        {
            get 
            { 
                return (Bytes[0] & 0x38) >> 3; 
            }
            set 
            { 
                Bytes[0] = (byte)((Bytes[0] & ~0x38) | value << 3); 
            }
        }

        /// <summary>
        /// Mode
        /// </summary>
        public NtpMode Mode
        {
            get 
            {
                // Isolate bits 0 - 3
                return (NtpMode)(Bytes[0] & 0x07); 
            }
            set 
            { 
                Bytes[0] = (byte)((Bytes[0] & ~0x07) | (byte)value); 
            }
        }

        /// <summary>
        /// Stratum
        /// </summary>
        public NtpStratum Stratum
        {
            get
            {
                byte val = (byte)Bytes[1];
                if (val == 0) return NtpStratum.Unspecified;
                else
                    if (val == 1) return NtpStratum.PrimaryReference;
                else
                    if (val <= 15) return NtpStratum.SecondaryReference;
                else
                    return NtpStratum.Reserved;
            }
            set
            {
                Bytes[1] = (byte)value;
            }
        }

        /// <summary>
        /// Poll Interval (in seconds)
        /// </summary>
        public uint PollInterval
        {
            get
            {
                // Thanks to Jim Hollenhorst <hollenho@attbi.com>
                return (uint)(Math.Pow(2, (sbyte)Bytes[2]));
            }
        }

        /// <summary>
        /// Precision (in seconds)
        /// </summary>
        public double Precision
        {
            get
            {
                // Thanks to Jim Hollenhorst <hollenho@attbi.com>
                return (Math.Pow(2, (sbyte)Bytes[3]));
            }
        }

        /// <summary>
        /// Root Delay
        /// </summary>
        public TimeSpan RootDelay => GetTimeSpan32(4);

        /// <summary>
        /// Root Dispersion
        /// </summary>
        public TimeSpan RootDispersion => GetTimeSpan32(8);

        /// <summary>
        /// Reference Identifier
        /// </summary>
        public uint ReferenceId => GetUInt32BE(12);

        /// <summary>
        /// Reference Timestamp
        /// </summary>
        public DateTime? ReferenceTimestamp
        {
            get 
            { 
                return GetDateTime64(16); 
            }
            set 
            { 
                SetDateTime64(16, value); 
            }
        }

        /// <summary>
        /// Originate Timestamp (T1)
        /// </summary>
        public DateTime? OriginTimestamp
        {
            get 
            { 
                return GetDateTime64(24); 
            }
            set 
            { 
                SetDateTime64(24, value); 
            }
        }

        /// <summary>
        /// Receive Timestamp (T2)
        /// </summary>
        public DateTime? ReceiveTimestamp
        {
            get 
            { 
                return GetDateTime64(32); 
            }
            set 
            { 
                SetDateTime64(32, value); 
            }
        }

        /// <summary>
        /// Transmit Timestamp (T3)
        /// </summary>
        public DateTime? TransmitTimestamp
        {
            get 
            { 
                return GetDateTime64(40); 
            }
            set 
            { 
                SetDateTime64(40, value); 
            }
        }

        /// <summary>
        /// Destination Timestamp (T4)
        /// </summary>
        public DateTime? DestinationTimestamp { get; set; }

        /// <summary>
        /// Round trip delay
        /// </summary>
        public TimeSpan RoundTripTime
        {
            get
            {
                return (DestinationTimestamp.Value - OriginTimestamp.Value) - (ReceiveTimestamp.Value - TransmitTimestamp.Value);
            }
        }

        /// <summary>
        /// Local clock offset
        /// </summary>
        public TimeSpan Offset
        {
            get
            {
                return TimeSpan.FromTicks(((ReceiveTimestamp.Value - OriginTimestamp.Value) - (DestinationTimestamp.Value - TransmitTimestamp.Value)).Ticks / 2);
            }
        }

        public NtpPacket() : this(new byte[Length])
        {
            Mode = NtpMode.Client;
            Version = 4;
            TransmitTimestamp = DateTime.UtcNow;
        }

        public NtpPacket(byte[] bytes)
        {
            if (bytes.Length < Length) throw new ArgumentException($"NTP Packet must be at least {Length} bytes long");
            Bytes = bytes;
        }

        DateTime? GetDateTime64(int offset)
        {
            var field = GetUInt64BE(offset);
            if (field == 0) return null;
            return new DateTime(NtpPacket.NtpEpochTime.Ticks + Convert.ToInt64(field * (1.0 / (1L << 32) * 10000000.0)), DateTimeKind.Utc);
        }

        void SetDateTime64(int offset, DateTime? value) 
        { 
            SetUInt64BE(offset, value == null ? 0 : Convert.ToUInt64((value.Value.Ticks - NtpPacket.NtpEpochTime.Ticks) * (0.0000001 * (1L << 32)))); 
        }

        TimeSpan GetTimeSpan32(int offset) 
        { 
            return TimeSpan.FromSeconds(GetInt32BE(offset) / (double)(1 << 16)); 
        }

        ulong GetUInt64BE(int offset) 
        { 
            return SwapEndianness(BitConverter.ToUInt64(Bytes, offset)); 
        }

        void SetUInt64BE(int offset, ulong value) 
        { 
            Array.Copy(BitConverter.GetBytes(SwapEndianness(value)), 0, Bytes, offset, 8); 
        }
        int GetInt32BE(int offset) 
        { 
            return (int)GetUInt32BE(offset); 
        }

        uint GetUInt32BE(int offset) 
        { 
            return SwapEndianness(BitConverter.ToUInt32(Bytes, offset)); 
        }

        static uint SwapEndianness(uint x) 
        { 
            return ((x & 0xff) << 24) | ((x & 0xff00) << 8) | ((x & 0xff0000) >> 8) | ((x & 0xff000000) >> 24); 
        }

        static ulong SwapEndianness(ulong x) 
        { 
            return ((ulong)SwapEndianness((uint)x) << 32) | SwapEndianness((uint)(x >> 32)); 
        }

        public static IPAddress UInt32ToIPAddress(UInt32 address)
        {
            return new IPAddress(new byte[] {
                (byte)((address>>24) & 0xFF) ,
                (byte)((address>>16) & 0xFF) ,
                (byte)((address>>8)  & 0xFF) ,
                (byte)( address & 0xFF)});
        }


        // Converts the object to string
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Reference server: { Server?.ToString() } ");
            sb.AppendLine($"The current time is { TransmitTimestamp?.ToLocalTime().ToString("o") }");
            sb.AppendLine($" { DestinationTimestamp?.ToString("HH:mm:ss") } { Offset }");
            
            sb.AppendLine("[NTP Packet]");
            sb.Append("Leap Indicator: ");
            switch (LeapIndicator)
            {
                case NtpLeapIndicator.NoWarning:
                    sb.AppendLine("No warning");
                    break;
                case NtpLeapIndicator.LastMinuteHas61Seconds:
                    sb.AppendLine("Last minute has 61 seconds");
                    break;
                case NtpLeapIndicator.LastMinuteHas59Seconds:
                    sb.AppendLine("Last minute has 59 seconds");
                    break;
                case NtpLeapIndicator.AlarmCondition:
                    sb.AppendLine("Alarm Condition (clock not synchronized)");
                    break;
            }
            sb.AppendLine("Version number: " + Version.ToString());
            sb.Append("Mode: ");
            switch (Mode)
            {
                case NtpMode.Unknown:
                    sb.AppendLine("Unknown");
                    break;
                case NtpMode.SymmetricActive:
                    sb.AppendLine("Symmetric Active");
                    break;
                case NtpMode.SymmetricPassive:
                    sb.AppendLine("Symmetric Pasive");
                    break;
                case NtpMode.Client:
                    sb.AppendLine("Client");
                    break;
                case NtpMode.Server:
                    sb.AppendLine("Server");
                    break;
                case NtpMode.Broadcast:
                    sb.AppendLine("Broadcast");
                    break;
            }
            sb.Append("Stratum: ");
            switch (Stratum)
            {
                case NtpStratum.Unspecified:
                case NtpStratum.Reserved:
                    sb.AppendLine("Unspecified");
                    break;
                case NtpStratum.PrimaryReference:
                    sb.AppendLine("Primary Reference");
                    break;
                case NtpStratum.SecondaryReference:
                    sb.AppendLine("Secondary Reference");
                    break;
            }
            sb.AppendLine($"Poll Interval: { PollInterval } s");
            sb.AppendLine($"Precision: { Precision } s");

            sb.AppendLine($"Reference ID: { UInt32ToIPAddress(ReferenceId) }");
            sb.AppendLine($"Root Delay: { RootDelay.TotalMilliseconds } ms");
            sb.AppendLine($"Root Dispersion: { RootDispersion.TotalMilliseconds } ms");

            sb.AppendLine($"Reference Timestamp: { ReferenceTimestamp?.ToLocalTime().ToString("o") }");
            sb.AppendLine($"Originate Timestamp: { OriginTimestamp?.ToLocalTime().ToString("o") }");
            sb.AppendLine($"Receive Timestamp: { ReceiveTimestamp?.ToLocalTime().ToString("o") }");
            sb.AppendLine($"Transmit Timestamp: { TransmitTimestamp?.ToLocalTime().ToString("o") }");

            sb.AppendLine("[non-NTP Packet]");
            sb.AppendLine($"Destination Timestamp: Roundtrip Delay: { RoundTripTime.TotalMilliseconds } ms");
            sb.AppendLine($"Local Clock Offset: { Offset } ");

            return sb.ToString();
        }
    }
}
