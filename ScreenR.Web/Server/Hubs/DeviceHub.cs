using Microsoft.AspNetCore.SignalR;
using ScreenR.Shared;
using ScreenR.Shared.Models;
using ScreenR.Web.Server.Models;
using System.Collections.Concurrent;

namespace ScreenR.Web.Server.Hubs
{
    public interface IDeviceHubClient
    {
        Task StartDesktopStream();
    }

    public class DeviceHub : Hub<IDeviceHubClient>
    {
        private static readonly ConcurrentDictionary<Guid, StreamingSession> _streamingSessions = new();

        private readonly ILogger<DeviceHub> _logger;

        public DeviceHub(ILogger<DeviceHub> logger)
        {
            _logger = logger;
        }

        public DeviceInfo DeviceInfo
        {
            get
            {
                if (Context.Items[nameof(DeviceInfo)] is DeviceInfo deviceInfo)
                {
                    return deviceInfo;
                }
                return DeviceInfo.Empty;
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

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SetDeviceInfo(DeviceInfo deviceInfo)
        {
            DeviceInfo = deviceInfo;

            switch (deviceInfo.Type)
            {
                case Shared.Enums.ConnectionType.Unknown:
                case Shared.Enums.ConnectionType.User:
                    _logger.LogWarning("Unexpected connection type: {type}", deviceInfo.Type);
                    return;
                case Shared.Enums.ConnectionType.Service:
                    await Groups.AddToGroupAsync(Context.ConnectionId, deviceInfo.DeviceId.ToString());
                    break;
                case Shared.Enums.ConnectionType.Desktop:
                    await Groups.AddToGroupAsync(Context.ConnectionId, deviceInfo.SessionId.ToString());
                    break;
                default:
                    break;
            }
        }

        internal static async Task<Result<IAsyncEnumerable<byte>>> GetStreamSession(Guid sessionId, TimeSpan timeout)
        {
            var session = _streamingSessions.GetOrAdd(sessionId, key => new StreamingSession(sessionId));
            var waitResult = await session.ReadySignal.WaitAsync(timeout);

            if (!waitResult)
            {
                return Result.Fail<IAsyncEnumerable<byte>>("Timed out while waiting for session.");
            }

            if (session.Stream is null)
            {
                return Result.Fail<IAsyncEnumerable<byte>>("Stream failed to start.");
            }

            return Result.Ok(session.Stream);
        }
    }
}
