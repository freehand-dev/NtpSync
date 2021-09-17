using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtpClient;
using NtpClient.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TimeSync.Models;

namespace TimeSync.Services
{
    public class TimeSyncService : BackgroundService, IDisposable
    {
        private readonly ILogger<TimeSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly AppSettings _settings;
        private PeriodicTimer _timer;
        Stopwatch _sw = new Stopwatch();

        public TimeSyncService(
            IServiceProvider serviceProvider,
            IOptions<AppSettings> settings,
            ILogger<TimeSyncService> logger)
        {
            this._logger = logger;
            this._serviceProvider = serviceProvider;
            this._settings = settings.Value;

            _timer = new PeriodicTimer(
                TimeSpan.FromSeconds(
                    this._settings.NtpClient.UpdateInterval));
        }

        public override void Dispose()
        {
            _timer?.Dispose();       
        }

        public override async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"TimeSync Service started at: { DateTime.UtcNow.ToString("o")}");
            _logger.LogInformation($"TimeSync Service settings: { JsonSerializer.Serialize(this._settings) }");
            
            await base.StartAsync(stoppingToken);
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TimeSync Service is stopping.");

            await base.StopAsync(stoppingToken);
        }

        private async Task<NtpPacket> QueryAsync()
        {
            object _lock = new object();

            // create ntp clients
            List<NtpClient.NtpClient> ntpClients = new List<NtpClient.NtpClient>();
            foreach (string peer in this._settings.NtpClient.Peers)
            {
                try
                {
                    ntpClients.Add(
                        new NtpClient.NtpClient(peer));
                }
                catch (System.TimeoutException e)
                {
                    _logger.LogError("[ExecuteAsync][NextTickAsync][Exception]: {1}", e);
                }
                catch (Exception e)
                {
                    _logger.LogError("[ExecuteAsync][NextTickAsync][Exception]: {1}", e);
                }
            }

            // Query NTP servers
            List<NtpPacket> ntpPackets = new List<NtpPacket>();
            foreach (NtpClient.NtpClient ntpClient in ntpClients)
            {
                try
                {
                    NtpPacket _ntpPacket = await ntpClient.QueryAsync();
                    lock (_lock)
                    {
                        ntpPackets.Add(_ntpPacket);
                    }
                    _logger.LogDebug("[{peer}][Offset]: {1}", _ntpPacket.Server.ToString(), _ntpPacket.Offset.TotalMilliseconds);
                }
                catch (Exception e)
                {
                    _logger.LogError("[ExecuteAsync][NextTickAsync][Exception]: {1}", e);
                }
            }

            return (from x in ntpPackets
                             orderby x.Stratum.ToReliable() ascending, Math.Abs(x.Offset.TotalMilliseconds) ascending
                             select x).FirstOrDefault();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            

            while (await _timer.WaitForNextTickAsync())
            {
                _logger.LogDebug("[ExecuteAsync][NextTickAsync][TimeStamp]: {time}", DateTime.UtcNow.ToString("o"));
                _sw.Restart();
                try
                {
                    NtpPacket ntpPacket = await this.QueryAsync();
                    string ntpServer = ntpPacket.Server.ToString();

                    _logger.LogDebug("[{peer}][Offset]: {1}", ntpServer, ntpPacket.ToString());

                    TimeSpan offset = (TimeSpan)ntpPacket?.Offset + TimeSpan.FromMilliseconds(_settings.Global.SystemClockOffset);

                    _logger.LogDebug("[{peer}][Offset + SystemClockOffset]: {1}", ntpServer, Math.Abs(offset.TotalMilliseconds));

                    if (offset.TotalMilliseconds > 0 && Math.Abs(offset.TotalMilliseconds) > _settings.NtpClient.MaxPosPhaseCorrection)
                    {
                        _logger.LogWarning("[{peer}]: Detect MaxPosPhaseCorrection: {1} - {2}", ntpServer, ntpPacket.Offset.TotalMilliseconds, _settings.NtpClient.MaxPosPhaseCorrection);
                    }

                    if ((offset.TotalMilliseconds) < 0 && Math.Abs(offset.TotalMilliseconds) > _settings.NtpClient.MaxNegPhaseCorrection)
                    {
                        _logger.LogWarning("[{peer}]: Detect MaxNegPhaseCorrection: {1} - {2}", ntpServer, ntpPacket.Offset.TotalMilliseconds, _settings.NtpClient.MaxNegPhaseCorrection);
                    }

                    if (Math.Abs(offset.TotalMilliseconds) > _settings.NtpClient.MaxAllowedPhaseOffset)
                    {
                        try
                        {
                            DateTime beforeTime = DateTime.UtcNow;
                            
                            _logger.LogDebug("[ExecuteAsync][NextTickAsync][AdjustClock][Befor]: {1}", beforeTime.ToString("o"));
                            
                            SystemClock.AdjustClock(offset);
                            
                            _logger.LogInformation("[ExecuteAsync][NextTickAsync][AdjustClock]: Change from {1} to {2}, ({3})", 
                                beforeTime.ToString("o"), 
                                DateTime.UtcNow.ToString("o"),
                                offset);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("[ExecuteAsync][NextTickAsync][AdjustClock][Exception]: {1}", e);
                        }
                        finally
                        {
                            _logger.LogDebug("[ExecuteAsync][NextTickAsync][AdjustClock][After]: {1}", DateTime.UtcNow.ToString("o"));
                        }
                    }
                }

                catch (System.TimeoutException e)
                {
                    _logger.LogError("[ExecuteAsync][NextTickAsync][TimeoutException]: {1}", e);
                }
                catch (Exception e)
                {
                    _logger.LogError("[ExecuteAsync][NextTickAsync][Exception]: {1}", e);
                }

                _logger.LogDebug("[ExecuteAsync][NextTickAsync][Elapsed]: {1}", _sw.Elapsed.TotalMilliseconds);
            }
        }
    }
}
