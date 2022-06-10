using Microsoft.AspNetCore.SignalR;
using ScreenR.Shared;
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

        public Guid SessionData
        {
            get
            {
                if (Context.Items[nameof(SessionData)] is Guid sessionId)
                {
                    return sessionId;
                }
                return Guid.Empty;
            }
            set
            {
                Context.Items[nameof(SessionData)] = value;
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

        public async Task SetSessionData(Guid sessionId)
        {
            SessionData = sessionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
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
