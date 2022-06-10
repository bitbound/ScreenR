using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Service.Interfaces;
using ScreenR.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Service.Services
{
    [SupportedOSPlatform("windows")]
    internal class InstallerServiceWindows : IInstallerService
    {
        private readonly string _installDir = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\", "Program Files", "ScreenR");
        private readonly IAppState _appState;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<InstallerServiceWindows> _logger;

        public InstallerServiceWindows(IAppState appState, IHostApplicationLifetime lifetime, ILogger<InstallerServiceWindows> logger)
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

                if (!CheckIsAdministrator())
                {
                    _logger.LogError("Install command must be run as administrator.");
                }

                var result = await ProcessHelper.GetProcessOutput("cmd.exe", "/c sc.exe stop ScreenR_Service");

                if (!result.IsSuccess)
                {
                    _logger.LogError("{msg}", result.Error);
                    return;
                }

                var procs = Process
                    .GetProcessesByName("ScreenR_Service")
                    .Where(x => x.Id != Environment.ProcessId);

                foreach (var proc in procs)
                {
                    proc.Kill();
                }

                var exePath = Environment.GetCommandLineArgs().First();
                var fileName = Path.GetFileName(exePath);
                var targetPath = Path.Combine(_installDir, fileName);
                Directory.CreateDirectory(_installDir);
                File.Copy(exePath, targetPath, true);

                result = await ProcessHelper.GetProcessOutput("cmd.exe", $"/c sc.exe create ScreenR_Service " +
                    $"binPath= \"\\\"{targetPath}\\\" run -s {_appState.ServerUrl} -d {_appState.DeviceId}\" start= auto");

                if (!result.IsSuccess)
                {
                    _logger.LogError("{msg}", result.Error);
                    return;
                }

                result = await ProcessHelper.GetProcessOutput("cmd.exe", "/c sc.exe failure \"ScreenR_Service\" reset= 5 actions= restart/5000");

                await ProcessHelper.GetProcessOutput("sc", "start ScreenR_Service");

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

        public async Task Uninstall()
        {
            try
            {
                _logger.LogInformation("Uninstall started.");

                var result = await ProcessHelper.GetProcessOutput("cmd.exe", "/c sc.exe stop ScreenR_Service");
                if (!result.IsSuccess)
                {
                    _logger.LogError("{msg}", result.Error);
                    return;
                }

                result = await ProcessHelper.GetProcessOutput("cmd.exe", "/c sc.exe delete ScreenR_Service");
                if (!result.IsSuccess)
                {
                    _logger.LogError("{msg}", result.Error);
                    return;
                }


                var procs = Process
                  .GetProcessesByName("ScreenR_Service")
                  .Where(x => x.Id != Environment.ProcessId);

                foreach (var proc in procs)
                {
                    proc.Kill();
                }


                _logger.LogInformation("Uninstall completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while uninstalling the ScreenR service.");
            }
            finally
            {
                _lifetime.StopApplication();
            }
        }

        private static bool CheckIsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
