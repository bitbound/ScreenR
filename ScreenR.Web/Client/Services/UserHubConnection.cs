using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Interfaces;
using ScreenR.Shared.Models;
using ScreenR.Shared.Services;
using System.Net;

namespace ScreenR.Web.Client.Services
{
    public interface IUserHubConnection : IUserHubClient
    {
        event EventHandler<DesktopDevice>? DesktopDeviceUpdated;
        event EventHandler<ServiceDevice>? ServiceDeviceUpdated;

        Task Connect();
        IAsyncEnumerable<DesktopFrameChunk> GetDesktopStream(Guid sessionId, Guid requestId, string passphrase = "");
        Task RequestDesktopStream(Guid deviceId, Guid requestId);
    }

    public class UserHubConnection : IUserHubConnection
    {
        private readonly HubConnection _connection;
        private readonly IHttpMessageHandlerFactory _handlerFactory;
        private readonly IToastService _toastService;
        private readonly ILogger<UserHubConnection> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        public UserHubConnection(
            IServiceScopeFactory scopeFactory,
            IWebAssemblyHostEnvironment hostEnv,
            IHttpMessageHandlerFactory handlerFactory,
            IHubConnectionBuilderFactory builderFactory,
            IToastService toastService,
            ILogger<UserHubConnection> logger)
        {
            _scopeFactory = scopeFactory;
            _handlerFactory = handlerFactory;
            _toastService = toastService;
            _logger = logger;

            _connection = builderFactory.CreateBuilder()
               .WithUrl($"{hostEnv.BaseAddress.TrimEnd('/')}/user-hub", options =>
               {
                   //options.HttpMessageHandlerFactory = (x) =>
                   //{
                   //    return _handlerFactory.CreateHandler("ScreenR.Web.ServerAPI");
                   //};
                   options.AccessTokenProvider = async () => {
                       using var scope = _scopeFactory.CreateScope();
                       var tokenProvider = scope.ServiceProvider.GetRequiredService<IAccessTokenProvider>();
                       var result = await tokenProvider.RequestAccessToken();
                       if (result.TryGetToken(out var token))
                       {
                           return token.Value;
                       }
                       return string.Empty;
                   };
               })
               .ConfigureLogging(x =>
               {
                   if (Uri.TryCreate(hostEnv.BaseAddress, UriKind.Absolute, out var result) && result.IsLoopback)
                   {
                       x.SetMinimumLevel(LogLevel.Debug);
                   }
               })
               .AddMessagePackProtocol()
               .WithAutomaticReconnect(new RetryPolicy())
               .Build();
        }

        public event EventHandler<DesktopDevice>? DesktopDeviceUpdated;
        public event EventHandler<ServiceDevice>? ServiceDeviceUpdated;

        public async Task Connect()
        {
            if (_connection.State != HubConnectionState.Disconnected)
            {
                return;
            }

            _connection.Reconnecting += HubConnection_Reconnecting;
            _connection.Reconnected += HubConnection_Reconnected;

            _connection.On<DesktopDevice>(nameof(NotifyDesktopDeviceUpdated), NotifyDesktopDeviceUpdated);
            _connection.On<ServiceDevice>(nameof(NotifyServiceDeviceUpdated), NotifyServiceDeviceUpdated);
            _connection.On<string, MessageLevel>(nameof(ShowToast), ShowToast);

            while (true)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var authProvider = scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
                    var authState = await authProvider.GetAuthenticationStateAsync();

                    if (authState.User.Identity?.IsAuthenticated == true)
                    {
                        await _connection.StartAsync();
                        _logger.LogInformation("Connected to server.");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in user hub connection.");
                }
                await Task.Delay(3_000);
            }
        }

        public async IAsyncEnumerable<DesktopFrameChunk> GetDesktopStream(Guid sessionId, Guid requestId, string passphrase = "")
        {
            await foreach (var chunk in _connection.StreamAsync<DesktopFrameChunk>("GetDesktopStream", sessionId, requestId, passphrase))
            {
                yield return chunk;
            }
        }

        public Task NotifyDesktopDeviceUpdated(DesktopDevice device)
        {
            TryInvoke(() => DesktopDeviceUpdated?.Invoke(this, device));
            return Task.CompletedTask;
        }

        public Task NotifyServiceDeviceUpdated(ServiceDevice device)
        {
            TryInvoke(() => ServiceDeviceUpdated?.Invoke(this, device));
            return Task.CompletedTask;
        }

        public async Task RequestDesktopStream(Guid deviceId, Guid requestId)
        {
            await _connection.InvokeAsync(nameof(RequestDesktopStream), deviceId, requestId);
        }

        public Task ShowToast(string message, MessageLevel messageLevel)
        {
            _toastService.ShowToast(message, messageLevel);
            return Task.CompletedTask;
        }

        private Task HubConnection_Reconnected(string? arg)
        {
            _logger.LogInformation("Reconnected to user hub.");
            return Task.CompletedTask;
        }

        private Task HubConnection_Reconnecting(Exception? arg)
        {
            _logger.LogWarning(arg, "Reconnecting to user hub.");
            return Task.CompletedTask;
        }

        private void TryInvoke(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while invoking hub client method.");
            }
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
