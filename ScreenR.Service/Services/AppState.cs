using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Service.Services
{
    internal interface IAppState
    {
        Guid DeviceId { get; }
        Uri ServerUrl { get; }
    }

    internal class AppState : IAppState
    {
        public AppState(Uri serverUrl, Guid deviceId)
        {
            ServerUrl = serverUrl;
            DeviceId = deviceId;
        }

        public Guid DeviceId { get; }
        public Uri ServerUrl { get; }
    }
}
