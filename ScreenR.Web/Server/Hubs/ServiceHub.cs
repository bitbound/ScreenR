using Microsoft.AspNetCore.SignalR;

namespace ScreenR.Web.Server.Hubs
{
    public interface IServiceHubClient
    {

    }

    public class ServiceHub : Hub<IServiceHubClient>
    {
    }
}
