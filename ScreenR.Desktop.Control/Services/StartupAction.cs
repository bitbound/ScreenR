using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Control.Services
{
    internal class StartupAction : IHostedService
    {
        private readonly IDesktopHubConnection _hubConnection;

        public StartupAction(IDesktopHubConnection hubConnection)
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
