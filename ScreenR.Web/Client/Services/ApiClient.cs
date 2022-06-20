using ScreenR.Shared;
using ScreenR.Shared.Models;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace ScreenR.Web.Client.Services
{
    public interface IApiClient
    {
        Task<Result<DesktopDevice[]>> GetDesktopDevices();
        Task<Result<ServiceDevice[]>> GetServiceDevices();
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

        public async Task<Result<DesktopDevice[]>> GetDesktopDevices()
        {
            return await TryAndLog(async () =>
                await _client.GetFromJsonAsync<DesktopDevice[]>("api/devices/desktop") ?? Array.Empty<DesktopDevice>());
        }

        public async Task<Result<bool>> CheckIfExistingUsers()
        {
            return await TryAndLog(async () =>
                await _client.GetFromJsonAsync<bool>("api/users/any"));
        }


        public async Task<Result<ServiceDevice[]>> GetServiceDevices()
        {
            return await TryAndLog(async () =>
                await _client.GetFromJsonAsync<ServiceDevice[]>("api/devices/service") ?? Array.Empty<ServiceDevice>());
        }


        private async Task<Result<T>> TryAndLog<T>(Func<Task<T>> invokeAction, [CallerMemberName] string methodName = "")
        {
            try
            {
                var results = await invokeAction();
                return Result.Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling {method}.", methodName);
                return Result.Fail<T>(ex);
            }
        }
    }
}
