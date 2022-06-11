using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;

namespace ScreenR.Web.Client.Services
{
    public interface IUserHubConnection
    {
        Task Connect();
    }

    public class UserHubConnection : IUserHubConnection
    {
        private readonly IWebAssemblyHostEnvironment _hostEnv;
        private readonly IHubConnectionBuilder _hubConnectionBuilder;
        private readonly ILogger<UserHubConnection> _logger;
        private HubConnection? _hubConnection;

        public UserHubConnection(
            IWebAssemblyHostEnvironment hostEnv,
            IHubConnectionBuilder hubConnectionBuilder,
            ILogger<UserHubConnection> logger)
        {
            _hostEnv = hostEnv;
            _hubConnectionBuilder = hubConnectionBuilder;
            _logger = logger;
        }

        public async Task Connect()
        {
            if (_hubConnection is not null)
            {
                return;
            }

            _hubConnection = _hubConnectionBuilder
               .WithUrl($"{_hostEnv.BaseAddress.TrimEnd('/')}/user-hub")
               .AddMessagePackProtocol()
               .WithAutomaticReconnect(new RetryPolicy())
               .Build();

            _hubConnection.Reconnecting += HubConnection_Reconnecting;
            _hubConnection.Reconnected += HubConnection_Reconnected;

            while (true)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    _logger.LogInformation("Connected to server.");
                    break;
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
