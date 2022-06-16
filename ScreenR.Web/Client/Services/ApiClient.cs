using ScreenR.Shared.Models;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace ScreenR.Web.Client.Services
{
    public interface IApiClient
    {
        ValueTask<DesktopDevice[]> GetDesktopDevices();
        ValueTask<ServiceDevice[]> GetServiceDevices();
    }

    public class ApiClient : IApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
        {
            _client = httpClient;
            _logger = logger;
        }

        public async ValueTask<DesktopDevice[]> GetDesktopDevices()
        {
            try
            {
                return await _client.GetFromJsonAsync<DesktopDevice[]>("api/devices/desktop") ?? Array.Empty<DesktopDevice>();
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw;
            }
        }

        public async ValueTask<ServiceDevice[]> GetServiceDevices()
        {
            try
            {
                return await _client.GetFromJsonAsync<ServiceDevice[]>("api/devices/service") ?? Array.Empty<ServiceDevice>();
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw;
            }
        }

        private void LogError(Exception ex, [CallerMemberName] string methodName = "")
        {
            _logger.LogError(ex, "Error while calling {method}.", methodName);
        }
    }
}
