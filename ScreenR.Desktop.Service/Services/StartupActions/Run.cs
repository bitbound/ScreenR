using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Service.Services.StartupActions
{
    internal class Run : IHostedService
    {
        private readonly IServiceHubConnection _hubConnection;

        public Run(IServiceHubConnection hubConnection)
        {
            _hubConnection = hubConnection;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _hubConnection.Connect();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
