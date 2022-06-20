using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScreenR.Desktop.Shared.Enums;
using ScreenR.Desktop.Shared.Interfaces;
using ScreenR.Desktop.Shared.Models;
using ScreenR.Web.Server.Data;
using ScreenR.Web.Server.Services;
using System.Collections.Concurrent;

namespace ScreenR.Web.Server.Hubs
{
    public class ServiceHub : Hub<IServiceHubClient>
    {
        private readonly AppDb _appDb;
        private readonly IHubContext<UserHub, IUserHubClient> _userHubContext;
        private readonly IDeviceConnectionsCache _deviceCache;
        private readonly ILogger<ServiceHub> _logger;

        public ServiceHub(
            AppDb appDb,
            IHubContext<UserHub, IUserHubClient> userHubContext,
            IDeviceConnectionsCache deviceCache,
            ILogger<ServiceHub> logger)
        {
            _appDb = appDb;
            _userHubContext = userHubContext;
            _deviceCache = deviceCache;
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
                _deviceCache.RemoveServiceDevice(device);
                device.IsOnline = false;
                device.LastOnline = DateTimeOffset.Now;
                device = await UpdateDeviceInDb(device);
                await _userHubContext.Clients.All.NotifyServiceDeviceUpdated(device);
            }

            await base.OnDisconnectedAsync(exception);
        }
        public async Task SetDeviceInfo(ServiceDevice device)
        {
            switch (device.Type)
            {
                case ConnectionType.Unknown:
                case ConnectionType.User:
                case ConnectionType.Desktop:
                default:
                    _logger.LogWarning("Unexpected connection type: {type}", device.Type);
                    return;
                case ConnectionType.Service:
                    await Groups.AddToGroupAsync(Context.ConnectionId, device.DeviceId.ToString());
                    break;
            }

            device.LastOnline = DateTimeOffset.Now;
            device = await UpdateDeviceInDb(device);
            _deviceCache.AddServiceDevice(device);
            DeviceInfo = device;
            await _userHubContext.Clients.All.NotifyServiceDeviceUpdated(device);
        }

        private async Task<ServiceDevice> UpdateDeviceInDb(ServiceDevice device)
        {
            var dbEntity = await _appDb.Devices.FirstOrDefaultAsync(x => x.DeviceId == device.DeviceId);

            if (dbEntity is not null)
            {
                device.Id = dbEntity.Id;
                var entry = _appDb.Entry(dbEntity);
                entry.CurrentValues.SetValues(device);
                entry.State = EntityState.Modified;
            }
            else
            {
                var result = await _appDb.Devices.AddAsync(device);
                dbEntity = result.Entity;
            }

            await _appDb.SaveChangesAsync();

            return dbEntity;
        }
    }
}
