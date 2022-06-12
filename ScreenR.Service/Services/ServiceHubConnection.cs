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

namespace ScreenR.Service.Services
{
    internal class ServiceHubConnection : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubConnectionBuilder _hubConnectionBuilder;
        private readonly IAppState _appState;
        private readonly ILogger<ServiceHubConnection> _logger;

        public ServiceHubConnection(
             IServiceScopeFactory scopeFactory,
             IHubConnectionBuilder hubConnectionBuilder,
             IAppState appState,
             ILogger<ServiceHubConnection> logger)
        {
            _scopeFactory = scopeFactory;
            _hubConnectionBuilder = hubConnectionBuilder;
            _appState = appState;
            _logger = logger;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_appState.ServerUrl is null)
            {
                throw new ArgumentNullException(nameof(_appState.ServerUrl));
            }

            var hubConnection = _hubConnectionBuilder
              .WithUrl($"{_appState.ServerUrl.Trim()}/service-hub")
              .AddMessagePackProtocol()
              .WithAutomaticReconnect(new RetryPolicy())
              .Build();

            hubConnection.Reconnecting += HubConnection_Reconnecting; ;
            hubConnection.Reconnected += HubConnection_Reconnected;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await hubConnection.StartAsync(stoppingToken);
                    _logger.LogInformation("Connected to server.");

                    var deviceInfo = DeviceInfo.Create(ConnectionType.Service, true, _appState.DeviceId, Guid.Empty);

                    await hubConnection.SendAsync("SetDeviceInfo", deviceInfo, cancellationToken: stoppingToken);
                    break;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning("Failed to connect to server.  Status Code: {code}", ex.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in device hub connection.");
                    await Task.Delay(3_000, stoppingToken);
                }
            }
        }

        private Task HubConnection_Reconnected(string? arg)
        {
            _logger.LogInformation("Reconnected to device hub.");
            return Task.CompletedTask;
        }

        private Task HubConnection_Reconnecting(Exception? arg)
        {
            _logger.LogWarning(arg, "Reconnecting to device hub.");
            return Task.CompletedTask;
        }

        private class RetryPolicy : IRetryPolicy
        {
            public TimeSpan? NextRetryDelay(RetryContext retryContext)
            {
                var waitSeconds = Math.Min(30, Math.Pow(retryContext.PreviousRetryCount, 2));
                return TimeSpan.FromSeconds(waitSeconds);
            }
        }
    }
}
