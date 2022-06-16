using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScreenR.Shared.Models;
using ScreenR.Web.Server.Services;

namespace ScreenR.Web.Server.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceConnectionsCache _deviceCache;

        public DevicesController(IDeviceConnectionsCache deviceCache)
        {
            _deviceCache = deviceCache;
        }


        [HttpGet("desktop")]
        public IEnumerable<DesktopDevice> GetDesktopDevices()
        {
            return _deviceCache.GetDesktopDevices();
        }

        [HttpGet("service")]
        public IEnumerable<ServiceDevice> GetServiceDevices()
        {
            return _deviceCache.GetServiceDevices();
        }
    }
}
