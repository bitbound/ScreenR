﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Interfaces;
using ScreenR.Shared.Models;
using ScreenR.Web.Server.Data;
using ScreenR.Web.Server.Services;

namespace ScreenR.Web.Server.Hubs
{
    [Authorize]
    public class UserHub : Hub<IUserHubClient>
    {
        private readonly IDeviceConnectionsCache _connectionCache;
        private readonly IHubContext<DesktopHub, IDesktopHubClient> _desktopHubContext;
        private readonly ILogger<UserHub> _logger;
        private readonly IHubContext<ServiceHub, IServiceHubClient> _serviceHubContext;
        public UserHub(
            IHubContext<DesktopHub, IDesktopHubClient> deviceHubContext,
            IHubContext<ServiceHub, IServiceHubClient> serviceHubContext,
            IDeviceConnectionsCache connectionsCache,
            ILogger<UserHub> logger)
        {
            _desktopHubContext = deviceHubContext;
            _serviceHubContext = serviceHubContext;
            _connectionCache = connectionsCache;
            _logger = logger;
        }

        public async IAsyncEnumerable<DesktopFrameChunk> GetDesktopStream(Guid sessionId, Guid requestId, string passphrase)
        {
            var streamToken = new StreamToken(sessionId, requestId);

            await _desktopHubContext.Clients
                .Groups(sessionId.ToString())
                .StartDesktopStream(streamToken, passphrase);

            var result = await DesktopHub.GetStreamSession(streamToken, TimeSpan.FromSeconds(30));

            if (!result.IsSuccess || result.Value?.Stream is null)
            {
                _logger.LogError("Failed to get streaming session. Error: {msg}", result.Error);
                yield break;
            }

            try
            {
                await foreach (var chunk in result.Value.Stream)
                {
                    yield return chunk;
                }
            }
            finally
            {
                result.Value.EndSignal.Release();
                _logger.LogInformation("Streaming session ended for session {sessionId}, request {requestId}.",
                    sessionId,
                    requestId);
            }

        }

        public async Task GetDisplays(Guid sessionId, Guid requestId)
        {
            await _desktopHubContext.Clients
               .Group(sessionId.ToString())
               .GetDisplays(requestId, Context.ConnectionId);
        }

        public async Task NotifyFrameReceived(Guid sessionId, Guid requestId)
        {
            await _desktopHubContext.Clients
              .Groups(sessionId.ToString())
              .FrameReceived(new StreamToken(sessionId, requestId));
        }
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public async Task<bool> RequestDesktopStream(Guid deviceId, Guid sessionId)
        {
            await _serviceHubContext.Clients
                .Group(deviceId.ToString())
                .RequestDesktopStream(sessionId, Context.ConnectionId);


            return await _connectionCache.WaitForDesktopDevice(sessionId, TimeSpan.FromSeconds(30));
        }

        public async Task RequestWindowsSessions(ConnectionType connectionType, Guid deviceOrSessionId, Guid requestId)
        {
            switch (connectionType)
            {
                case ConnectionType.Unknown:
                case ConnectionType.User:
                    _logger.LogWarning("Unexpected connection type: {type}", connectionType);
                    break;
                case ConnectionType.Service:
                    await _serviceHubContext.Clients
                        .Group(deviceOrSessionId.ToString())
                        .RequestWindowsSessions(requestId, Context.ConnectionId);
                    break;
                case ConnectionType.Desktop:
                    await _desktopHubContext.Clients
                        .Group(deviceOrSessionId.ToString())
                        .RequestWindowsSessions(requestId, Context.ConnectionId);
                    break;
                default:
                    break;
            }
        }
    }
}
