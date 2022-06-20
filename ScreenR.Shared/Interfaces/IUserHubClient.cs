using ScreenR.Desktop.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Shared.Interfaces
{
    public interface IUserHubClient
    {
        Task NotifyDesktopDeviceUpdated(DesktopDevice device);
        Task NotifyServiceDeviceUpdated(ServiceDevice device);
    }
}
