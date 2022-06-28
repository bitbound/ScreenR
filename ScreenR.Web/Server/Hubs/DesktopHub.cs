using Microsoft.AspNetCore.SignalR;
using ScreenR.Shared;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Enums;
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
        private static readonly ConcurrentDictionary<StreamToken, StreamingSession> _streamingSessions = new();
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
                await _userHubContext.Clients.All.NotifyDesktopDeviceUpdated(device);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendDtoToUser(byte[] dto, string userConnectionId)
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

            await _userHubContext.Clients.All.NotifyDesktopDeviceUpdated(device);
        }

        public async Task SendDesktopStream(StreamToken streamToken, IAsyncEnumerable<DesktopFrameChunk> stream)
        {
            var session = _streamingSessions.GetOrAdd(streamToken, key => new StreamingSession(streamToken));

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

        internal static async Task<Result<StreamingSession>> GetStreamSession(StreamToken streamToken, TimeSpan timeout)
        {
            var session = _streamingSessions.GetOrAdd(streamToken, key => new StreamingSession(streamToken));
            var waitResult = await session.ReadySignal.WaitAsync(timeout);

            if (!waitResult)
            {
                return Result.Fail<StreamingSession>("Timed out while waiting for session.");
            }

            if (session.Stream is null)
            {
                return Result.Fail<StreamingSession>("Stream failed to start.");
            }

            return Result.Ok(session);
        }
    }
}
