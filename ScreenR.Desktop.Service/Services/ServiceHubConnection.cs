using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Desktop.Shared;
using ScreenR.Desktop.Shared.Services;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Extensions;
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
        HubConnection Connection { get; }
        Task Connect();
    }

    internal class ServiceHubConnection : IServiceHubConnection, IServiceHubClient
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IAppState _appState;
        private readonly IDeviceCreator _deviceCreator;
        private readonly IProcessLauncher _processLauncher;
        private readonly ILogger<ServiceHubConnection> _logger;
        public HubConnection Connection { get; }

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

            Connection = builderFactory.CreateBuilder()
                .WithUrl($"{_appState.ServerUrl.Trim()}/service-hub")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();
        }


        public async Task Connect()
        {

            Connection.Reconnecting += HubConnection_Reconnecting; ;
            Connection.Reconnected += HubConnection_Reconnected;
            Connection.On<Guid, string>("RequestDesktopStream", OnRequestDesktopStream);

            while (!_appLifetime.ApplicationStopping.IsCancellationRequested)
            {
                try
                {
                    await Connection.StartAsync(_appLifetime.ApplicationStopping);
                    _logger.LogInformation("Connected to server.");

                    var deviceInfo = _deviceCreator.CreateService(_appState.DeviceId, true);

                    await Connection.SendAsync("SetDeviceInfo", deviceInfo, cancellationToken: _appLifetime.ApplicationStopping);
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

        private async Task OnRequestDesktopStream(Guid requestId, string requesterConnectionId)
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
                        await Connection.InvokeAsync("SendToast", "Failed to download remote control app.", MessageLevel.Error, requesterConnectionId);
                        return;
                    }
                    using var fs = new FileStream(FileNames.RemoteControl, FileMode.Create);
                    await stream.CopyToAsync(fs);
                }

                await Connection.InvokeAsync("SendToast", "Remote control starting", MessageLevel.Success, requesterConnectionId);
                await _processLauncher.LaunchDesktopStreamer(_appState.ServerUrl.ToString(), requestId, requesterConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while requesting desktop stream.");
            }
        }

        private async Task HubConnection_Reconnected(string? arg)
        {
            var deviceInfo = _deviceCreator.CreateService(_appState.DeviceId, true);
            await Connection.SendAsync("SetDeviceInfo", deviceInfo);
            _logger.LogInformation("Reconnected to device hub.");
        }

        private Task HubConnection_Reconnecting(Exception? arg)
        {
            _logger.LogWarning(arg, "Reconnecting to device hub.");
            return Task.CompletedTask;
        }

        public async Task RequestDesktopStream(Guid requestId, string requesterConnectionId)
        {
            await _processLauncher.LaunchDesktopStreamer(_appState.ServerUrl.ToString(), requestId, requesterConnectionId);
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
