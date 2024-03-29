﻿using Microsoft.AspNetCore.SignalR;
using ScreenR.Shared;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Helpers;
using ScreenR.Shared.Interfaces;
using ScreenR.Shared.Models;
using ScreenR.Web.Server.Data;
using ScreenR.Web.Server.Models;
using ScreenR.Web.Server.Services;
using System.Collections.Concurrent;

namespace ScreenR.Web.Server.Hubs
{

    public class DesktopHub : Hub<IDesktopHubClient>
    {
        private static readonly ConcurrentDictionary<StreamToken, StreamSignaler> _streamingSessions = new();
        private readonly IHubContext<UserHub, IUserHubClient> _userHubContext;
        private readonly IDeviceConnectionsCache _deviceCache;
        private readonly ILogger<DesktopHub> _logger;

        public DesktopHub(
            IHubContext<UserHub, IUserHubClient> userHubContext,
            IDeviceConnectionsCache deviceCache,
            ILogger<DesktopHub> logger)
        {
            _userHubContext = userHubContext;
            _deviceCache = deviceCache;
            _logger = logger;
        }

        public DesktopDevice? DeviceInfo
        {
            get
            {
                if (Context.Items[nameof(DeviceInfo)] is DesktopDevice deviceInfo)
                {
                    return deviceInfo;
                }
                return null;
            }
            set
            {
                Context.Items[nameof(DeviceInfo)] = value;
            }
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (DeviceInfo is DesktopDevice device)
            {
                device.LastOnline = DateTimeOffset.Now;
                _deviceCache.RemoveDesktopDevice(device);
                device.IsOnline = false;

                foreach (var wrapper in DtoChunker.ChunkDto(device, DtoType.DesktopDeviceUpdated))
                {
                    await _userHubContext.Clients.All.ReceiveDto(wrapper);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendDtoToUser(DtoWrapper dto, string userConnectionId)
        {
            await _userHubContext.Clients.Client(userConnectionId).ReceiveDto(dto);
        }

        public async Task SetDeviceInfo(DesktopDevice device)
        {
            device.LastOnline = DateTimeOffset.Now;
            DeviceInfo = device;
            _deviceCache.AddDesktopDevice(device);

            switch (device.Type)
            {
                case ConnectionType.Unknown:
                case ConnectionType.User:
                case ConnectionType.Service:
                    _logger.LogWarning("Unexpected connection type: {type}", device.Type);
                    break;
                case ConnectionType.Desktop:
                    await Groups.AddToGroupAsync(Context.ConnectionId, device.SessionId.ToString());
                    break;
                default:
                    _logger.LogWarning("Unexpected connection type: {type}", device.Type);
                    break;
            }

            foreach (var wrapper in DtoChunker.ChunkDto(device, DtoType.DesktopDeviceUpdated))
            {
                await _userHubContext.Clients.All.ReceiveDto(wrapper);
            }
        }

        public async Task SendDesktopStream(StreamToken streamToken, IAsyncEnumerable<DesktopFrameChunk> stream)
        {
            var session = _streamingSessions.GetOrAdd(streamToken, key => new StreamSignaler(streamToken));

            try
            {
                session.Stream = stream;
                session.ReadySignal.Release();
                await session.EndSignal.WaitAsync(TimeSpan.FromHours(8));
            }
            finally
            {
                _streamingSessions.TryRemove(session.StreamToken, out _);
            }
        }

        public async Task SendToast(string message, MessageLevel messageLevel, string userConnectionId)
        {
            await _userHubContext.Clients.Client(userConnectionId).ShowToast(message, messageLevel);
        }

        internal static async Task<Result<StreamSignaler>> GetStreamSession(StreamToken streamToken, TimeSpan timeout)
        {
            var session = _streamingSessions.GetOrAdd(streamToken, key => new StreamSignaler(streamToken));
            var waitResult = await session.ReadySignal.WaitAsync(timeout);

            if (!waitResult)
            {
                return Result.Fail<StreamSignaler>("Timed out while waiting for session.");
            }

            if (session.Stream is null)
            {
                return Result.Fail<StreamSignaler>("Stream failed to start.");
            }

            return Result.Ok(session);
        }
    }
}
