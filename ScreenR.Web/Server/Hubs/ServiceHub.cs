using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScreenR.Shared.Interfaces;
using ScreenR.Shared.Models;
using ScreenR.Web.Server.Data;
using System.Collections.Concurrent;

namespace ScreenR.Web.Server.Hubs
{
    public class ServiceHub : Hub<IServiceHubClient>
    {
        private readonly AppDb _appDb;
        private readonly IHubContext<UserHub, IUserHubClient> _userHubContext;
        private readonly ILogger<ServiceHub> _logger;

        public ServiceHub(
            AppDb appDb,
            IHubContext<UserHub, IUserHubClient> userHubContext,
            ILogger<ServiceHub> logger)
        {
            _appDb = appDb;
            _userHubContext = userHubContext;
            _logger = logger;
        }

        public ServiceDevice? DeviceInfo
        {
            get
            {
                if (Context.Items[nameof(DeviceInfo)] is ServiceDevice deviceInfo)
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

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (DeviceInfo is ServiceDevice device)
            {
                device.IsOnline = false;
                device.LastOnline = DateTimeOffset.Now;
                await UpdateDeviceInDb(device);
                await _userHubContext.Clients.All.NotifyServiceDeviceUpdated(device);
            }

            await base.OnDisconnectedAsync(exception);
        }
        public async Task SetDeviceInfo(ServiceDevice device)
        {
            switch (device.Type)
            {
                case Shared.Enums.ConnectionType.Unknown:
                case Shared.Enums.ConnectionType.User:
                case Shared.Enums.ConnectionType.Desktop:
                    _logger.LogWarning("Unexpected connection type: {type}", device.Type);
                    break;
                case Shared.Enums.ConnectionType.Service:
                    await Groups.AddToGroupAsync(Context.ConnectionId, device.DeviceId.ToString());
                    break;
                default:
                    _logger.LogWarning("Unexpected connection type: {type}", device.Type);
                    break;
            }

            device.LastOnline = DateTimeOffset.Now;
            await UpdateDeviceInDb(device);
            DeviceInfo = device;
            await _userHubContext.Clients.All.NotifyServiceDeviceUpdated(device);
        }

        private async Task UpdateDeviceInDb(ServiceDevice device)
        {
            var existingEntity = await _appDb.Devices.FindAsync(device.Id);

            if (existingEntity is not null)
            {
                var entry = _appDb.Entry(existingEntity);
                entry.CurrentValues.SetValues(device);
                entry.State = EntityState.Modified;
            }
            else
            {
                await _appDb.Devices.AddAsync(device);
            }

            await _appDb.SaveChangesAsync();
        }
    }
}
