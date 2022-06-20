using Microsoft.Extensions.Hosting;
using ScreenR.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Service.Services
{
    internal class UpdaterService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (EnvironmentHelper.IsDebug)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckForUpdate();
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }

        private Task CheckForUpdate()
        {
            return Task.CompletedTask;
        }
    }
}
