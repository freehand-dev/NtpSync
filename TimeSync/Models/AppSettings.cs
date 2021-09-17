using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSync.Models
{
    public class AppSettings
    {
        public GlobalSettings Global { get; set; }
        public NtpClientSettings NtpClient { get; set; }
        public AppSettings()
        {
            this.Global = new GlobalSettings();

            this.NtpClient = new NtpClientSettings();
        }
    }
    public class GlobalSettings
    {
        /// <summary>
        /// 
        /// </summary>
        public int SystemClockOffset { get; set; } = 0;
    }

    public class NtpClientSettings
    {

        private int _updateInterval = 300;

        /// <summary>
        /// 
        /// </summary>
        public int UpdateInterval
        {
            get => (_updateInterval <= 1) ? 1 : _updateInterval;
            set => this._updateInterval = (value <= 1) ? 1 : value;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<string> Peers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int MaxAllowedPhaseOffset { get; set; } = 40;


        /// <summary>
        /// 
        /// </summary>
        public int MaxPosPhaseCorrection { get; set; } = 5000;


        /// <summary>
        /// 
        /// </summary>
        public int MaxNegPhaseCorrection { get; set; } = 5000;



    }
}
