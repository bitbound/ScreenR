using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Extensions;
using ScreenR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Core.Services
{
    internal class DesktopHubConnection : BackgroundService
    {
        private readonly IHubConnectionBuilder _hubConnectionBuilder;
        private readonly IAppState _appState;
        private readonly ILogger<DesktopHubConnection> _logger;
        private readonly HubConnection _hubConnection;

        public DesktopHubConnection(
            IHubConnectionBuilder hubConnectionBuilder,
            IAppState appState,
            ILogger<DesktopHubConnection> logger)
        {
            _hubConnectionBuilder = hubConnectionBuilder;
            _appState = appState;
            _logger = logger;

            _hubConnection = _hubConnectionBuilder
                .WithUrl($"{_appState.ServerUrl.Trim()}/device-hub")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();
        }

        private async Task StartDesktopStream(string passphrase)
        {
            if (passphrase != _appState.Passphrase)
            {
                _logger.LogWarning("Invalid passphrase supplied: {passphrase}", passphrase);
                return;
            }

            async IAsyncEnumerable<byte> SendStream()
            {
                for (var i = 0; i < 100_000; i++)
                {
                    yield return (byte)new Random().Next(0, 255);
                }
                await Task.Delay(1);
            }

            await _hubConnection.SendAsync("SendDesktopStream", SendStream());
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _hubConnection.Reconnecting += HubConnection_Reconnecting;
            _hubConnection.Reconnected += HubConnection_Reconnected;
            _hubConnection.On<string>("StartDesktopStream", StartDesktopStream);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Connecting to server.");

                    await _hubConnection.StartAsync(stoppingToken);

                    _logger.LogInformation("Connected to server.");

                    var deviceInfo = DeviceInfo.Create(ConnectionType.Desktop, true, Guid.Empty, _appState.SessionId);

                    await _hubConnection.SendAsync("SetDeviceInfo", deviceInfo, cancellationToken: stoppingToken);

                    _logger.LogInformation("Created session: {sessionId}", _appState.SessionId);
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

        private Task HubConnection_Reconnected(string? arg)
        {
            _logger.LogInformation("Reconnected to desktop hub.");
            return Task.CompletedTask;
        }

        private Task HubConnection_Reconnecting(Exception? arg)
        {
            _logger.LogWarning(arg, "Reconnecting to desktop hub.");
            return Task.CompletedTask;
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
