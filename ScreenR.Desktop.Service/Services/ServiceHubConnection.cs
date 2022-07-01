using MessagePack;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Desktop.Shared;
using ScreenR.Desktop.Shared.Native.Windows;
using ScreenR.Desktop.Shared.Services;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Extensions;
using ScreenR.Shared.Helpers;
using ScreenR.Shared.Interfaces;
using ScreenR.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Service.Services
{
    internal interface IServiceHubConnection
    {
        Task Connect();
    }

    internal class ServiceHubConnection : IServiceHubConnection
    {
        public readonly HubConnection _connection;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IAppState _appState;
        private readonly IDeviceCreator _deviceCreator;
        private readonly ILogger<ServiceHubConnection> _logger;
        private readonly IProcessLauncher _processLauncher;

        public ServiceHubConnection(
             IHostApplicationLifetime appLifetime,
             IHubConnectionBuilderFactory builderFactory,
             IAppState appState,
             IDeviceCreator deviceCreator,
             IProcessLauncher processLauncher,
             ILogger<ServiceHubConnection> logger)
        {
            _appLifetime = appLifetime;
            _appState = appState;
            _deviceCreator = deviceCreator;
            _processLauncher = processLauncher;
            _logger = logger;

            _connection = builderFactory.CreateBuilder()
                .WithUrl($"{_appState.ServerUrl.Trim()}/service-hub")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();
        }

        public async Task Connect()
        {

            _connection.Reconnecting += HubConnection_Reconnecting; ;
            _connection.Reconnected += HubConnection_Reconnected;
            _connection.On<Guid, string>(nameof(IServiceHubClient.RequestDesktopStream), RequestDesktopStream);
            _connection.On<Guid, string>(nameof(IServiceHubClient.RequestWindowsSessions), RequestWindowsSessions);

            while (!_appLifetime.ApplicationStopping.IsCancellationRequested)
            {
                try
                {
                    await _connection.StartAsync(_appLifetime.ApplicationStopping);
                    _logger.LogInformation("Connected to server.");

                    var deviceInfo = _deviceCreator.CreateService(_appState.DeviceId, true);

                    await _connection.SendAsync("SetDeviceInfo", deviceInfo, cancellationToken: _appLifetime.ApplicationStopping);
                    break;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning("Failed to connect to server.  Status Code: {code}", ex.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in device hub connection.");
                    await Task.Delay(3_000, _appLifetime.ApplicationStopping);
                }
            }
        }

        private async Task HubConnection_Reconnected(string? arg)
        {
            var deviceInfo = _deviceCreator.CreateService(_appState.DeviceId, true);
            await _connection.SendAsync("SetDeviceInfo", deviceInfo);
            _logger.LogInformation("Reconnected to device hub.");
        }

        private Task HubConnection_Reconnecting(Exception? arg)
        {
            _logger.LogWarning(arg, "Reconnecting to device hub.");
            return Task.CompletedTask;
        }

        private async Task RequestDesktopStream(Guid requestId, string requesterConnectionId)
        {
            try
            {
                if (!File.Exists(FileNames.RemoteControl))
                {
                    using var client = new HttpClient();
                    using var stream = await client.GetStreamAsync($"{_appState.ServerUrl}/Downloads/{FileNames.RemoteControl}");
                    if (stream is null)
                    {
                        _logger.LogError("Stream is null while downloading remote control.");
                        await _connection.InvokeAsync("SendToast", "Download failure on client", MessageLevel.Error, requesterConnectionId);
                        return;
                    }
                    using var fs = new FileStream(FileNames.RemoteControl, FileMode.Create);
                    await stream.CopyToAsync(fs);
                }

                await _connection.InvokeAsync("SendToast", "Remote control starting", MessageLevel.Success, requesterConnectionId);
                await _processLauncher.LaunchDesktopStreamer(_appState.ServerUrl.ToString(), requestId, requesterConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while requesting desktop stream.");
            }
        }

        private async void RequestWindowsSessions(Guid requestId, string requesterConnectionId)
        {
            if (EnvironmentHelper.Platform != Platform.Windows)
            {
                _logger.LogWarning("Received request for Windows sessions, but platform is {platform}.", EnvironmentHelper.Platform);
                return;
            }

            var sessions = Win32Interop.GetActiveSessions();
            await SendDtoToUser(sessions, DtoType.WindowsSessions, requestId, requesterConnectionId);
        }

        private async Task SendDtoToUser<T>(T dto, DtoType dtoType, Guid requestId, string requesterConnectionId)
        {
            foreach (var wrapper in DtoChunker.ChunkDto(dto, dtoType, requestId))
            {
                await _connection.InvokeAsync("SendDtoToUser", wrapper, requesterConnectionId);
            }
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
