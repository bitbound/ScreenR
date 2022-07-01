using MessagePack;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using ScreenR.Shared;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Helpers;
using ScreenR.Shared.Interfaces;
using ScreenR.Shared.Models;
using ScreenR.Shared.Services;
using System.Net;

namespace ScreenR.Web.Client.Services
{
    public interface IUserHubConnection
    {
        event EventHandler<DesktopDevice>? DesktopDeviceUpdated;
        event EventHandler<ServiceDevice>? ServiceDeviceUpdated;

        Task Connect();
        IAsyncEnumerable<DesktopFrameChunk> GetDesktopStream(Guid sessionId, Guid requestId, string passphrase = "");
        Task<Result<List<DisplayDto>>> GetDisplays(Guid sessionId);
        Task RequestDesktopStream(Guid deviceId, Guid requestId);

        Task<Result<List<WindowsSession>>> RequestWindowsSessions(Device device);
    }

    public class UserHubConnection : HubConnectionBase, IUserHubConnection
    {
        private readonly HubConnection _connection;
        private readonly IHttpMessageHandlerFactory _handlerFactory;
        private readonly ILogger<UserHubConnection> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IToastService _toastService;

        public UserHubConnection(
            IServiceScopeFactory scopeFactory,
            IWebAssemblyHostEnvironment hostEnv,
            IHttpMessageHandlerFactory handlerFactory,
            IHubConnectionBuilderFactory builderFactory,
            IToastService toastService,
            ILogger<UserHubConnection> logger)
            : base(logger)
        {
            _scopeFactory = scopeFactory;
            _handlerFactory = handlerFactory;
            _toastService = toastService;
            _logger = logger;

            _connection = builderFactory.CreateBuilder()
               .WithUrl($"{hostEnv.BaseAddress.TrimEnd('/')}/user-hub", options =>
               {
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

            _connection.On<string, MessageLevel>(nameof(IUserHubClient.ShowToast), ShowToast);
            _connection.On<DtoWrapper>(nameof(IUserHubClient.ReceiveDto), ReceiveDto);

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

        private void ReceiveDto(DtoWrapper dto)
        {
            try
            {
                switch (dto.DtoType)
                {
                    case DtoType.DesktopDeviceUpdated:
                        {
                            if (DtoChunker.TryComplete<DesktopDevice>(dto, out var device) &&
                                device is not null)
                            {
                                TryInvoke(() => DesktopDeviceUpdated?.Invoke(this, device));
                            }

                            break;
                        }
                    case DtoType.ServiceDeviceUpdated:
                        {
                            if (DtoChunker.TryComplete<ServiceDevice>(dto, out var device) &&
                                device is not null)
                            {
                                TryInvoke(() => ServiceDeviceUpdated?.Invoke(this, device));
                            }

                            break;
                        }
                    case DtoType.Unknown:
                    case DtoType.DesktopFrameChunk:
                    case DtoType.WindowsSessions:
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while receiving DtoWrapper.");
            }
         
        }

        public async IAsyncEnumerable<DesktopFrameChunk> GetDesktopStream(Guid sessionId, Guid requestId, string passphrase = "")
        {
            await foreach (var chunk in _connection.StreamAsync<DesktopFrameChunk>("GetDesktopStream", sessionId, requestId, passphrase))
            {
                yield return chunk;
            }
        }

        public async Task<Result<List<DisplayDto>>> GetDisplays(Guid sessionId)
        {
            var requestId = Guid.NewGuid();

            var result = await WaitForResponse<List<DisplayDto>>(
                _connection,
                nameof(IUserHubClient.ReceiveDto),
                requestId,
                async () =>
                {
                    await _connection.InvokeAsync("GetDisplays", sessionId, requestId);
                });

            return result;
        }
        public async Task<Result<List<WindowsSession>>> RequestWindowsSessions(Device device)
        {
            try
            {
                var requestId = Guid.NewGuid();

                var result = await WaitForResponse<List<WindowsSession>>(
                    _connection,
                    nameof(IUserHubClient.ReceiveDto),
                    requestId,
                    async () =>
                    {
                        if (device is ServiceDevice serviceDevice)
                        {
                            await _connection.InvokeAsync("RequestWindowsSessions", device.Type, serviceDevice.DeviceId, requestId);
                        }
                        else if (device is DesktopDevice desktopDevice)
                        {
                            await _connection.InvokeAsync("RequestWindowsSessions", device.Type, desktopDevice.SessionId, requestId);
                        }
                    });

                if (!result.IsSuccess)
                {
                    if (result.Exception is not null)
                    {
                        _logger.LogError(result.Exception, "Error while getting Windows sessions.");
                    }
                    else
                    {
                        _logger.LogError("{msg}", result.Error);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting windows sessions.");
                return Result.Fail<List<WindowsSession>>(ex);
            }
        }

        public async Task RequestDesktopStream(Guid deviceId, Guid requestId)
        {
            await _connection.InvokeAsync(nameof(RequestDesktopStream), deviceId, requestId);
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

        private Task ShowToast(string message, MessageLevel messageLevel)
        {
            _toastService.ShowToast(message, messageLevel);
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
