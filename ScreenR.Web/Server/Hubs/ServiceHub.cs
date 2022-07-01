using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Helpers;
using ScreenR.Shared.Interfaces;
using ScreenR.Shared.Models;
using ScreenR.Web.Server.Data;
using ScreenR.Web.Server.Services;

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

                foreach (var wrapper in DtoChunker.ChunkDto(device, DtoType.ServiceDeviceUpdated))
                {
                    await _userHubContext.Clients.All.ReceiveDto(wrapper);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendToast(string message, MessageLevel messageLevel, string userConnectionId)
        {
            await _userHubContext.Clients.Client(userConnectionId).ShowToast(message, messageLevel);
        }

        public async Task SendDtoToUser(DtoWrapper dto, string userConnectionId)
        {
            await _userHubContext.Clients.Client(userConnectionId).ReceiveDto(dto);
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

            foreach (var wrapper in DtoChunker.ChunkDto(device, DtoType.ServiceDeviceUpdated))
            {
                await _userHubContext.Clients.All.ReceiveDto(wrapper);
            }
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
