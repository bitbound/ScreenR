using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Desktop.Core.Interfaces;
using ScreenR.Shared.Extensions;
using ScreenR.Shared.Interfaces;
using ScreenR.Shared.Models;
using ScreenR.Shared.Services;

namespace ScreenR.Desktop.Core.Services
{
    internal interface IDesktopHubConnection : IDesktopHubClient
    {
        Task Connect();
    }

    internal class DesktopHubConnection : IDesktopHubConnection
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IAppState _appState;
        private readonly HubConnection _connection;
        private readonly IDesktopStreamer _desktopStreamer;
        private readonly ILogger<DesktopHubConnection> _logger;
        private Device _deviceInfo;

        public DesktopHubConnection(
            IHostApplicationLifetime appLifetime,
            IAppState appState,
            IDesktopStreamer desktopStreamer,
            IHubConnectionBuilderFactory builderFactory,
            ILogger<DesktopHubConnection> logger)
        {
            _appLifetime = appLifetime;
            _appState = appState;
            _desktopStreamer = desktopStreamer;
            _logger = logger;
            _deviceInfo = Device.CreateDesktop(appState.DesktopId, true);

            _connection = builderFactory.CreateBuilder()
                .WithUrl($"{appState.ServerUrl.Trim()}/desktop-hub")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();
        }

        public async Task Connect()
        {
            _connection.Reconnecting += HubConnection_Reconnecting;
            _connection.Reconnected += HubConnection_Reconnected;

            _connection.On<StreamToken, string>(nameof(StartDesktopStream), StartDesktopStream);

            while (!_appLifetime.ApplicationStopping.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Connecting to server.");

                    await _connection.StartAsync(_appLifetime.ApplicationStopping);

                    _logger.LogInformation("Connected to server.");

                    await _connection.SendAsync("SetDeviceInfo", _deviceInfo, cancellationToken: _appLifetime.ApplicationStopping);

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

        public async Task StartDesktopStream(StreamToken streamToken, string passphrase)
        {
            if (passphrase != _appState.Passphrase)
            {
                _logger.LogWarning("Invalid passphrase supplied: {passphrase}", passphrase);
                return;
            }

            // TODO: Cancellation token.
            // TODO: Throttle sender.
            await _connection.SendAsync("SendDesktopStream", streamToken, _desktopStreamer.GetDesktopStream());
        }

        private async Task HubConnection_Reconnected(string? arg)
        {
            _deviceInfo = Device.CreateDesktop(_appState.DesktopId, true);
            await _connection.SendAsync("SetDeviceInfo", _deviceInfo);
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
