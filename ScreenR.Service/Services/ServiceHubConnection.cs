using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Extensions;
using ScreenR.Shared.Models;
using ScreenR.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Service.Services
{
    internal interface IServiceHubConnection
    {
        HubConnection Connection { get; }
        Task Connect();
    }

    internal class ServiceHubConnection : IServiceHubConnection
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IAppState _appState;
        private readonly IDeviceCreator _deviceCreator;
        private readonly ILogger<ServiceHubConnection> _logger;
        public HubConnection Connection { get; }

        public ServiceHubConnection(
             IHostApplicationLifetime appLifetime,
             IHubConnectionBuilderFactory builderFactory,
             IAppState appState,
             IDeviceCreator deviceCreator,
             ILogger<ServiceHubConnection> logger)
        {
            _appLifetime = appLifetime;
            _appState = appState;
            _deviceCreator = deviceCreator;
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
