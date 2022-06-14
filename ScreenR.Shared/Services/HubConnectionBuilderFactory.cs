using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Services
{
    public interface IHubConnectionBuilderFactory
    {
        IHubConnectionBuilder CreateBuilder();
    }

    internal class HubConnectionBuilderFactory : IHubConnectionBuilderFactory
    {
        public IHubConnectionBuilder CreateBuilder()
        {
            return new HubConnectionBuilder();
        }
    }
}
