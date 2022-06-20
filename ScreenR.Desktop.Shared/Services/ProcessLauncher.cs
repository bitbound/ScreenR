using Microsoft.Extensions.Logging;
using PInvoke;
using ScreenR.Desktop.Shared.Native.Windows;
using ScreenR.Shared;
using ScreenR.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Kernel32 = PInvoke.Kernel32;

namespace ScreenR.Desktop.Shared.Services
{
    public interface IProcessLauncher
    {
        Task<Result<string>> GetProcessOutput(string command, string arguments = "", int msTimeout = 10000);
        Task<Result> LaunchDesktopStreamer(string serverUrl, Guid requestId, string requesterConnectionId);
    }

    internal class ProcessLauncher : IProcessLauncher
    {
        private readonly ILogger<ProcessLauncher> _logger;

        public ProcessLauncher(ILogger<ProcessLauncher> logger)
        {
            _logger = logger;
        }

        public async Task<Result<string>> GetProcessOutput(string command, string arguments = "", int msTimeout = 10_000)
        {
            try
            {
                var psi = new ProcessStartInfo(command, arguments)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Verb = "RunAs",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                var proc = Process.Start(psi);

                if (proc is null)
                {
                    return Result.Fail<string>("Process failed to start.");
                }

                if (!proc.WaitForExit(msTimeout))
                {
                    return Result.Fail<string>("Process did not close in time.");
                }

                var output = await proc.StandardOutput.ReadToEndAsync();
                return Result.Ok(output);
            }
            catch (Exception ex)
            {
                return Result.Fail<string>(ex);
            }
        }

        public async Task<Result> LaunchDesktopStreamer(string serverUrl, Guid requestId, string requesterConnectionId)
        {
            try
            {
                switch (EnvironmentHelper.Platform)
                {
                    case ScreenR.Shared.Enums.Platform.Unknown:
                    case ScreenR.Shared.Enums.Platform.MacOS:
                    case ScreenR.Shared.Enums.Platform.MacCatalyst:
                    case ScreenR.Shared.Enums.Platform.Browser:
                    default:
                        break;
                    case ScreenR.Shared.Enums.Platform.Windows:
                        await LaunchDesktopStreamerWindows(serverUrl, requestId, requesterConnectionId);
                        break;
                    case ScreenR.Shared.Enums.Platform.Linux:
                        await LaunchDesktopStreamerLinux(serverUrl, requestId, requesterConnectionId);
                        break;
                }
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while launching desktop streamer.");
                return Result.Fail(ex);
            }
        }

        private Task LaunchDesktopStreamerLinux(string serverUrl, Guid requestId, string requesterConnectionId)
        {
            throw new NotImplementedException();
        }

        private async Task LaunchDesktopStreamerWindows(string serverUrl, Guid requestId, string requesterConnectionId)
        {
            var filename = "ScreenR.exe";
            var arguments = $"start -s {serverUrl} -i {requestId}";
            
            if (Process.GetCurrentProcess().SessionId == 0)
            {
                var result = Win32Interop.LaunchProcessInSession(
                    $"{filename} {arguments}",
                    -1,
                    false,
                    "Default",
                    true,
                    out var procInfo);
            }
            else
            {
                Process.Start(filename, arguments);
            }
        }
    }
}
