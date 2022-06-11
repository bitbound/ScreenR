using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Http;

namespace ScreenR.Web.Client.Services
{
    public interface IUserHubConnection
    {
        Task Connect();
        IAsyncEnumerable<byte> GetDesktopStream(Guid sessionId, string passphrase = "");
    }

    public class UserHubConnection : IUserHubConnection
    {
        private readonly ILogger<UserHubConnection> _logger;
        private readonly IHttpMessageHandlerFactory _handlerFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HubConnection _hubConnection;

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

        public async IAsyncEnumerable<byte> GetDesktopStream(Guid sessionId, string passphrase = "")
        {
            await foreach (var streamByte in _hubConnection.StreamAsync<byte>("GetDesktopStream", sessionId, passphrase))
            {
                yield return streamByte;
            }
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
