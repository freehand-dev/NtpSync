using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace NtpClient
{
    public class SystemClock
    {
        internal static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct SYSTEMTIME
            {
                public short wYear;
                public short wMonth;
                public short wDayOfWeek;
                public short wDay;
                public short wHour;
                public short wMinute;
                public short wSecond;
                public short wMilliseconds;
            }

            [DllImport("kernel32.dll", EntryPoint = "GetSystemTime", SetLastError = true)]
            public extern static void GetSystemTime(ref SYSTEMTIME sysTime);

            [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
            public extern static bool SetSystemTime(ref SYSTEMTIME sysTime);

            [DllImport("netapi32.dll", SetLastError = true, EntryPoint = "NetRemoteTOD", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            internal static extern int GetRemoteTime(string UncServerName, ref IntPtr BufferPtr);

            [DllImport("netapi32.dll", SetLastError = true, EntryPoint = "NetApiBufferFree")]
            internal static extern int NetApiBufferFree(IntPtr bufptr);
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct TimeOfDayInfo
        {
            public int Elapsed;
            public int Milliseconds;
            public int Hours;
            public int Minutes;
            public int Seconds;
            public int Hundredth;
            public int Timezone;
            public int Interval;
            public int Day;
            public int Month;
            public int Year;
            public int Weekday;
        }

        /// <summary>
        /// Systemzeit
        /// </summary>
        public static DateTime Clock
        {
            get
            {
                return DateTime.Now;
            }
            set
            {
                SystemClock.SetClock(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        public static DateTime GetRemoteTime(string hostName)
        {
            TimeOfDayInfo remoteTimeInfo = new TimeOfDayInfo();
            IntPtr remoteTimePtr = IntPtr.Zero;

            int result = NativeMethods.GetRemoteTime(hostName, ref remoteTimePtr);
            if (result > 0)
            {
                throw new Win32Exception(result);
            }

            remoteTimeInfo = (TimeOfDayInfo)Marshal.PtrToStructure(
             remoteTimePtr,
             typeof(TimeOfDayInfo));

            // Pointer wieder freigeben
            NativeMethods.NetApiBufferFree(remoteTimePtr);

            DateTime remoteTime = new DateTime(
             remoteTimeInfo.Year,
             remoteTimeInfo.Month,
             remoteTimeInfo.Day,
             remoteTimeInfo.Hours,
             remoteTimeInfo.Minutes,
             remoteTimeInfo.Seconds,
             remoteTimeInfo.Hundredth * 10, DateTimeKind.Utc);

            return remoteTime;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostName"></param>
        public static void Synchronize(string hostName)
        {
            SystemClock.Clock = SystemClock.GetRemoteTime(hostName);
        }


        [SecurityPermission(SecurityAction.LinkDemand)]
        public static void SetClock(DateTime dateTime)
        {
            if (dateTime.Kind != DateTimeKind.Utc)
                dateTime = dateTime.ToUniversalTime();

            NativeMethods.SYSTEMTIME s = new NativeMethods.SYSTEMTIME();
            s.wYear = Convert.ToInt16(dateTime.Year);
            s.wMonth = Convert.ToInt16(dateTime.Month);
            s.wDay = Convert.ToInt16(dateTime.Day);
            s.wHour = Convert.ToInt16(dateTime.Hour);
            s.wMinute = Convert.ToInt16(dateTime.Minute);
            s.wSecond = Convert.ToInt16(dateTime.Second);
            s.wMilliseconds = Convert.ToInt16(dateTime.Millisecond);
            s.wDayOfWeek = Convert.ToInt16(dateTime.DayOfWeek, CultureInfo.InvariantCulture);

            if (!NativeMethods.SetSystemTime(ref s))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }
        }

        public static void AdjustClock(TimeSpan offset)
        {
            SystemClock.SetClock(
                ((DateTime)(DateTime.Now + offset)));
        }

    }
}
