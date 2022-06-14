using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Models;
using ScreenR.Shared.Services;
using System.Net;

namespace ScreenR.Web.Client.Services
{
    public interface IUserHubConnection
    {
        HubConnection Connection { get; }
        Task Connect();
        IAsyncEnumerable<DesktopFrameChunk> GetDesktopStream(Guid sessionId, Guid requestId, string passphrase = "");
    }

    public class UserHubConnection : IUserHubConnection
    {
        private readonly IHttpMessageHandlerFactory _handlerFactory;
        private readonly ILogger<UserHubConnection> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        public UserHubConnection(
            IServiceScopeFactory scopeFactory,
            IWebAssemblyHostEnvironment hostEnv,
            IHttpMessageHandlerFactory handlerFactory,
            IHubConnectionBuilderFactory builderFactory,
            ILogger<UserHubConnection> logger)
        {
            _scopeFactory = scopeFactory;
            _handlerFactory = handlerFactory;
            _logger = logger;

            Connection = builderFactory.CreateBuilder()
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

        public HubConnection Connection { get; }
        public async Task Connect()
        {
            if (Connection.State != HubConnectionState.Disconnected)
            {
                return;
            }

            Connection.Reconnecting += HubConnection_Reconnecting;
            Connection.Reconnected += HubConnection_Reconnected;

            while (true)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var authProvider = scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
                    var authState = await authProvider.GetAuthenticationStateAsync();

                    if (authState.User.Identity?.IsAuthenticated == true)
                    {
                        await Connection.StartAsync();
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
            await foreach (var chunk in Connection.StreamAsync<DesktopFrameChunk>("GetDesktopStream", sessionId, requestId, passphrase))
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
