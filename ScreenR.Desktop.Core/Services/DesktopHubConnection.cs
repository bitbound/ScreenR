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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubConnectionBuilder _hubConnectionBuilder;
        private readonly IAppState _appState;
        private readonly ILogger<DesktopHubConnection> _logger;

        public DesktopHubConnection(
            IServiceScopeFactory scopeFactory, 
            IHubConnectionBuilder hubConnectionBuilder,
            IAppState appState,
            ILogger<DesktopHubConnection> logger)
        {
            _scopeFactory = scopeFactory;
            _hubConnectionBuilder = hubConnectionBuilder;
            _appState = appState;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var hubConnection = _hubConnectionBuilder
                .WithUrl($"{_appState.ServerUrl.Trim()}/device-hub")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();

            hubConnection.Reconnecting += HubConnection_Reconnecting;
            hubConnection.Reconnected += HubConnection_Reconnected;
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await hubConnection.StartAsync(stoppingToken);
                    _logger.LogInformation("Connected to server.");

                    var deviceInfo = DeviceInfo.Create(ConnectionType.Desktop, true, Guid.Empty, _appState.SessionId);

                    await hubConnection.SendAsync("SetDeviceInfo", deviceInfo, cancellationToken: stoppingToken);
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
