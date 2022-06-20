using ScreenR.Shared.Models;
using System.Collections.Concurrent;

namespace ScreenR.Web.Server.Services
{
    public interface IDeviceConnectionsCache
    {
        IEnumerable<DesktopDevice> GetDesktopDevices();
        IEnumerable<ServiceDevice> GetServiceDevices();
        void AddDesktopDevice(DesktopDevice device);
        void RemoveDesktopDevice(DesktopDevice device);
        void AddServiceDevice(ServiceDevice device);
        void RemoveServiceDevice(ServiceDevice device);
    }

    public class DeviceConnectionsCache : IDeviceConnectionsCache
    {
        private readonly ConcurrentDictionary<Guid, DesktopDevice> _desktopDevices = new();
        private readonly ConcurrentDictionary<Guid, ServiceDevice> _serviceDevices = new();

        public void AddDesktopDevice(DesktopDevice device)
        {
            _desktopDevices.AddOrUpdate(device.SessionId, device, (k, v) => device);
        }

        public void AddServiceDevice(ServiceDevice device)
        {
            _serviceDevices.AddOrUpdate(device.DeviceId, device, (k, v) => device);
        }

        public IEnumerable<DesktopDevice> GetDesktopDevices()
        {
            return _desktopDevices.Values;
        }

        public IEnumerable<ServiceDevice> GetServiceDevices()
        {
            return _serviceDevices.Values;
        }

        public void RemoveDesktopDevice(DesktopDevice device)
        {
            _desktopDevices.TryRemove(device.SessionId, out _);
        }

        public void RemoveServiceDevice(ServiceDevice device)
        {
            _serviceDevices.TryRemove(device.DeviceId, out _);
        }
    }
}
