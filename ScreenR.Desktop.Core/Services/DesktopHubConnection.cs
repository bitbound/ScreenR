using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Desktop.Core.Interfaces;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Extensions;
using ScreenR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Core.Services
{
    internal class DesktopHubConnection : BackgroundService
    {
        private readonly IAppState _appState;
        private readonly IDesktopStreamer _desktopStreamer;
        private readonly HubConnection _hubConnection;
        private readonly IHubConnectionBuilder _hubConnectionBuilder;
        private readonly ILogger<DesktopHubConnection> _logger;
        private DeviceInfo _deviceInfo;

        public DesktopHubConnection(
            IHubConnectionBuilder hubConnectionBuilder,
            IAppState appState,
            IDesktopStreamer desktopStreamer,
            ILogger<DesktopHubConnection> logger)
        {
            _hubConnectionBuilder = hubConnectionBuilder;
            _appState = appState;
            _desktopStreamer = desktopStreamer;
            _logger = logger;
            _deviceInfo = DeviceInfo.Create(ConnectionType.Desktop, true, Guid.Empty, appState.DesktopId);

            _hubConnection = _hubConnectionBuilder
                .WithUrl($"{appState.ServerUrl.Trim()}/desktop-hub")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _hubConnection.Reconnecting += HubConnection_Reconnecting;
            _hubConnection.Reconnected += HubConnection_Reconnected;
            _hubConnection.On<StreamToken, string>("StartDesktopStream", StartDesktopStream);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Connecting to server.");

                    await _hubConnection.StartAsync(stoppingToken);

                    _logger.LogInformation("Connected to server.");

                    await _hubConnection.SendAsync("SetDeviceInfo", _deviceInfo, cancellationToken: stoppingToken);

                    _logger.LogInformation("Created session with desktop process ID: {desktopId}", _appState.DesktopId);
                    break;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning("Failed to connect to server.  Status Code: {code}", ex.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in desktop hub connection.");
                }
                await Task.Delay(3_000, stoppingToken);
            }
        }

        private async Task HubConnection_Reconnected(string? arg)
        {
            _deviceInfo = DeviceInfo.Create(ConnectionType.Desktop, true, Guid.Empty, _appState.DesktopId);
            await _hubConnection.SendAsync("SetDeviceInfo", _deviceInfo);
            _logger.LogInformation("Reconnected to desktop hub.");
        }

        private Task HubConnection_Reconnecting(Exception? arg)
        {
            _logger.LogWarning(arg, "Reconnecting to desktop hub.");
            return Task.CompletedTask;
        }

        private async Task StartDesktopStream(StreamToken streamToken, string passphrase)
        {
            if (passphrase != _appState.Passphrase)
            {
                _logger.LogWarning("Invalid passphrase supplied: {passphrase}", passphrase);
                return;
            }

            // TODO: Cancellation token.
            // TODO: Throttle sender.
            await _hubConnection.SendAsync("SendDesktopStream", streamToken, _desktopStreamer.GetDesktopStream());
        }

        private class RetryPolicy : IRetryPolicy
        {
            public TimeSpan? NextRetryDelay(RetryContext retryContext)
            {
                return TimeSpan.FromSeconds(3);
            }
        }
    }
}
