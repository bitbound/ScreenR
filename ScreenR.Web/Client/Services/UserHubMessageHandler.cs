using Microsoft.AspNetCore.SignalR.Client;
using ScreenR.Shared.Interfaces;
using ScreenR.Shared.Models;

namespace ScreenR.Web.Client.Services
{
    public class UserHubMessageHandler : IUserHubClient
    {
        private readonly IUserHubConnection _hubConnection;

        public UserHubMessageHandler(IUserHubConnection userHubConnection)
        {
            _hubConnection = userHubConnection;

            _hubConnection.Connection.On<DesktopDevice>(nameof(NotifyDesktopDeviceUpdated), NotifyDesktopDeviceUpdated);
            _hubConnection.Connection.On<ServiceDevice>(nameof(NotifyServiceDeviceUpdated), NotifyServiceDeviceUpdated);
        }

        public Task NotifyDesktopDeviceUpdated(DesktopDevice device)
        {
            throw new NotImplementedException();
        }

        public Task NotifyServiceDeviceUpdated(ServiceDevice device)
        {
            throw new NotImplementedException();
        }
    }
}
