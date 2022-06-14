using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Service.Interfaces;
using ScreenR.Shared.Native.Linux;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Service.Services
{
    internal class InstallerServiceLinux : IInstallerService
    {
        private readonly string _installDir = "/usr/local/bin/screenr";
        private readonly IAppState _appState;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<InstallerServiceLinux> _logger;

        public InstallerServiceLinux(IAppState appState, IHostApplicationLifetime lifetime, ILogger<InstallerServiceLinux> logger)
        {
            _appState = appState;
            _lifetime = lifetime;
            _logger = logger;
        }


        public async Task Install()
        {
            try
            {
                _logger.LogInformation("Install started.");

                if (Libc.geteuid() != 0)
                {
                    _logger.LogError("Install command must be run with sudo.");
                }

                var exePath = Environment.GetCommandLineArgs().First();
                var fileName = Path.GetFileName(exePath);
                var targetPath = Path.Combine(_installDir, fileName);
                Directory.CreateDirectory(_installDir);
                File.Copy(exePath, targetPath, true);


                var serviceFile = GetServiceFile(targetPath).Trim();
                var serviceFilePath = Path.Combine(_installDir, "screenr.service");

                await File.WriteAllTextAsync(serviceFilePath, serviceFile);

                Process.Start("sudo", "systemctl enable screenr.service");
                Process.Start("sudo", "systemctl restart screenr.service");

                _logger.LogInformation("Install completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while installing the ScreenR service.");
            }
            finally
            {
                _lifetime.StopApplication();
            }
        }

        public Task Uninstall()
        {
            throw new NotImplementedException();
        }

        private string GetServiceFile(string binaryPath)
        {
            return @$"
                [Unit]
                Description=ScreenR provides remote access via SignalR streaming.

                [Service]
                WorkingDirectory=/usr/local/bin/screenr/
                ExecStart={binaryPath} run -s {_appState.ServerUrl} -d {_appState.DeviceId}
                Restart=always
                StartLimitIntervalSec=0
                RestartSec=10

                [Install]
                WantedBy=graphical.target
            ";
        }
    }
}
