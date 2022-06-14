using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Desktop.Core.Interfaces;
using ScreenR.Shared.Extensions;
using ScreenR.Shared.Models;
using ScreenR.Shared.Services;

namespace ScreenR.Desktop.Core.Services
{
    internal interface IDesktopHubConnection
    {
        HubConnection Connection { get; }

        Task Connect();
    }

    internal class DesktopHubConnection : IDesktopHubConnection
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IAppState _appState;
        private readonly ILogger<DesktopHubConnection> _logger;
        private Device _deviceInfo;

        public DesktopHubConnection(
            IHostApplicationLifetime appLifetime,
            IAppState appState,
            IHubConnectionBuilderFactory builderFactory,
            ILogger<DesktopHubConnection> logger)
        {
            _appLifetime = appLifetime;
            _appState = appState;
            _logger = logger;
            _deviceInfo = Device.CreateDesktop(appState.DesktopId, true);

            Connection = builderFactory.CreateBuilder()
                .WithUrl($"{appState.ServerUrl.Trim()}/desktop-hub")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();
        }

        public HubConnection Connection { get; }
        public async Task Connect()
        {
            Connection.Reconnecting += HubConnection_Reconnecting;
            Connection.Reconnected += HubConnection_Reconnected;

            while (!_appLifetime.ApplicationStopping.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Connecting to server.");

                    await Connection.StartAsync(_appLifetime.ApplicationStopping);

                    _logger.LogInformation("Connected to server.");

                    await Connection.SendAsync("SetDeviceInfo", _deviceInfo, cancellationToken: _appLifetime.ApplicationStopping);

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
                await Task.Delay(3_000, _appLifetime.ApplicationStopping);
            }
        }

        private async Task HubConnection_Reconnected(string? arg)
        {
            _deviceInfo = Device.CreateDesktop(_appState.DesktopId, true);
            await Connection.SendAsync("SetDeviceInfo", _deviceInfo);
            _logger.LogInformation("Reconnected to desktop hub.");
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
