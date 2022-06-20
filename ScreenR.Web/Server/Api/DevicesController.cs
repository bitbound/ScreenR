using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScreenR.Desktop.Shared.Models;
using ScreenR.Web.Server.Data;
using ScreenR.Web.Server.Services;

namespace ScreenR.Web.Server.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceConnectionsCache _deviceCache;
        private readonly AppDb _appDb;

        public DevicesController(AppDb appDb, IDeviceConnectionsCache deviceCache)
        {
            _deviceCache = deviceCache;
            _appDb = appDb;
        }


        [HttpGet("desktop")]
        public IEnumerable<DesktopDevice> GetDesktopDevices()
        {
            return _deviceCache.GetDesktopDevices();
        }

        [HttpGet("service")]
        public async Task<IEnumerable<ServiceDevice>> GetServiceDevices()
        {
            return await _appDb.Devices.ToListAsync();
        }
    }
}
