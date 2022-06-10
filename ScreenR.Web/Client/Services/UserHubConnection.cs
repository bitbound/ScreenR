using Microsoft.AspNetCore.SignalR.Client;

namespace ScreenR.Web.Client.Services
{
    public interface IUserHubConnection
    {

    }

    public class UserHubConnection : IUserHubConnection
    {
        private readonly IHubConnectionBuilder _hubConnectionBuilder;
        private readonly ILogger<UserHubConnection> _logger;

        public UserHubConnection(
            IServiceScopeFactory scopeFactory,
            IHubConnectionBuilder hubConnectionBuilder,
            ILogger<UserHubConnection> logger)
        {
            _hubConnectionBuilder = hubConnectionBuilder;
            _logger = logger;
        }

        public async Task Connect(CancellationToken cancelToken)
        {
            var hubConnection = _hubConnectionBuilder
                .WithUrl("/user-hub")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();

            hubConnection.Reconnecting += HubConnection_Reconnecting;
            hubConnection.Reconnected += HubConnection_Reconnected;

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    await hubConnection.StartAsync(cancelToken);
                    _logger.LogInformation("Connected to server.");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning("Failed to connect to server.  Status Code: {code}", ex.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in user hub connection.");
                    await Task.Delay(3_000, cancelToken);
                }
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
