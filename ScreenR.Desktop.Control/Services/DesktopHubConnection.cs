using MessagePack;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Desktop.Control.Interfaces;
using ScreenR.Desktop.Shared.Native.Windows;
using ScreenR.Desktop.Shared.Services;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Extensions;
using ScreenR.Shared.Helpers;
using ScreenR.Shared.Interfaces;
using ScreenR.Shared.Models;
using ScreenR.Shared.Services;

namespace ScreenR.Desktop.Control.Services
{
    internal interface IDesktopHubConnection
    {
        Task Connect();
    }

    internal class DesktopHubConnection : IDesktopHubConnection
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IAppState _appState;
        private readonly HubConnection _connection;
        private readonly IDesktopStreamer _desktopStreamer;
        private readonly IDeviceCreator _deviceCreator;
        private readonly ILogger<DesktopHubConnection> _logger;
        private DesktopDevice _deviceInfo;

        public DesktopHubConnection(
            IHostApplicationLifetime appLifetime,
            IAppState appState,
            IDesktopStreamer desktopStreamer,
            IHubConnectionBuilderFactory builderFactory,
            IDeviceCreator deviceCreator,
            ILogger<DesktopHubConnection> logger)
        {
            _appLifetime = appLifetime;
            _appState = appState;
            _desktopStreamer = desktopStreamer;
            _deviceCreator = deviceCreator;
            _logger = logger;
            _deviceInfo = _deviceCreator.CreateDesktop(appState.DesktopId, true);

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

            _connection.On<StreamToken, string>(nameof(IDesktopHubClient.StartDesktopStream), StartDesktopStream);
            _connection.On<Guid, string>(nameof(IDesktopHubClient.RequestWindowsSessions), RequestWindowsSessions);

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

        private async Task HubConnection_Reconnected(string? arg)
        {
            _deviceInfo = _deviceCreator.CreateDesktop(_appState.DesktopId, true);
            await _connection.SendAsync("SetDeviceInfo", _deviceInfo);
            _logger.LogInformation("Reconnected to desktop hub.");
        }

        private Task HubConnection_Reconnecting(Exception? arg)
        {
            _logger.LogWarning(arg, "Reconnecting to desktop hub.");
            return Task.CompletedTask;
        }

        private async void RequestWindowsSessions(Guid requestId, string requesterConnectionId)
        {
            if (EnvironmentHelper.Platform != ScreenR.Shared.Enums.Platform.Windows)
            {
                _logger.LogWarning("Received request for Windows sessions, but platform is {platform}.", EnvironmentHelper.Platform);
                return;
            }

            var sessions = Win32Interop.GetActiveSessions();
            await SendDtoToUser(DtoType.WindowsSessions, sessions, requestId, requesterConnectionId);
        }

        private async Task SendDtoToUser(DtoType dtoType, object dto, Guid requestId, string requesterConnectionId)
        {
            var serializedDto = MessagePackSerializer.Serialize(dto);

            var chunks = serializedDto.Chunk(50_000).ToArray();

            for (var i = 0; i < chunks.Length; i++)
            {
                var wrapper = new DtoWrapper()
                {
                    DtoChunk = chunks[i],
                    DtoType = dtoType,
                    IsFirstChunk = i == 0,
                    IsLastChunk = i == chunks.Length - 1,
                    RequestId = requestId
                };

                await _connection.InvokeAsync("SendDtoToUser", wrapper, requesterConnectionId);
            }
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
            await _connection.SendAsync("SendDesktopStream", streamToken, _desktopStreamer.GetDesktopStream());
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
