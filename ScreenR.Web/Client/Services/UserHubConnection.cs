using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Http;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Models;

namespace ScreenR.Web.Client.Services
{
    public interface IUserHubConnection
    {
        Task Connect();
        IAsyncEnumerable<DesktopFrameChunk> GetDesktopStream(Guid sessionId, Guid requestId, string passphrase = "");
    }

    public class UserHubConnection : IUserHubConnection
    {
        private readonly IHttpMessageHandlerFactory _handlerFactory;
        private readonly HubConnection _hubConnection;
        private readonly ILogger<UserHubConnection> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        public UserHubConnection(
            IServiceScopeFactory scopeFactory,
            IWebAssemblyHostEnvironment hostEnv,
            IHubConnectionBuilder hubConnectionBuilder,
            IHttpMessageHandlerFactory handlerFactory,
            ILogger<UserHubConnection> logger)
        {
            _scopeFactory = scopeFactory;
            _handlerFactory = handlerFactory;
            _logger = logger;

            _hubConnection = hubConnectionBuilder
               .WithUrl($"{hostEnv.BaseAddress.TrimEnd('/')}/user-hub", options =>
               {
                   options.HttpMessageHandlerFactory = (x) =>
                   {
                       return _handlerFactory.CreateHandler("ScreenR.Web.ServerAPI");
                   };
               })
               .AddMessagePackProtocol()
               .WithAutomaticReconnect(new RetryPolicy())
               .Build();
        }

        public async Task Connect()
        {
            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                return;
            }

            _hubConnection.Reconnecting += HubConnection_Reconnecting;
            _hubConnection.Reconnected += HubConnection_Reconnected;

            while (true)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var authProvider = scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
                    var authState = await authProvider.GetAuthenticationStateAsync();

                    if (authState.User.Identity?.IsAuthenticated == true)
                    {
                        await _hubConnection.StartAsync();
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
            await foreach (var chunk in _hubConnection.StreamAsync<DesktopFrameChunk>("GetDesktopStream", sessionId, requestId, passphrase))
            {
                yield return chunk;
            }
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
        private class RetryPolicy : IRetryPolicy
        {
            public TimeSpan? NextRetryDelay(RetryContext retryContext)
            {
                return TimeSpan.FromSeconds(3);
            }
        }
    }
}
