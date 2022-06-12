using Microsoft.AspNetCore.SignalR;
using ScreenR.Shared;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Models;
using ScreenR.Web.Server.Models;
using System.Collections.Concurrent;

namespace ScreenR.Web.Server.Hubs
{
    public interface IDesktopHubClient
    {
        Task StartDesktopStream(StreamToken streamToken, string passphrase);
    }

    public class DesktopHub : Hub<IDesktopHubClient>
    {
        private static readonly ConcurrentDictionary<StreamToken, StreamingSession> _streamingSessions = new();

        private readonly ILogger<DesktopHub> _logger;

        public DesktopHub(ILogger<DesktopHub> logger)
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
                case Shared.Enums.ConnectionType.Service:
                    _logger.LogWarning("Unexpected connection type: {type}", deviceInfo.Type);
                    break;
                case Shared.Enums.ConnectionType.Desktop:
                    await Groups.AddToGroupAsync(Context.ConnectionId, deviceInfo.DesktopId.ToString());
                    break;
                default:
                    _logger.LogWarning("Unexpected connection type: {type}", deviceInfo.Type);
                    break;
            }
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
