using ScreenR.Shared;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace ScreenR.Web.Client.Services
{
    public interface IAnonymousApiClient
    {
        Task<Result<bool>> CheckIfExistingUsers();
    }

    public class AnonymousApiClient : IAnonymousApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<AnonymousApiClient> _logger;

        public AnonymousApiClient(IHttpClientFactory clientFactory, ILogger<AnonymousApiClient> logger)
        {
            _client = clientFactory.CreateClient(nameof(IAnonymousApiClient));
            _logger = logger;
        }

        public async Task<Result<bool>> CheckIfExistingUsers()
        {
            return await TryAndLog(async () =>
                await _client.GetFromJsonAsync<bool>("api/users/any"));
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
