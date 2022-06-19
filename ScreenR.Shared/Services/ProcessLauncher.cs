using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Services
{
    public interface IProcessLauncher
    {
        Task<Result<string>> GetProcessOutput(string command, string arguments = "", int msTimeout = 10000);
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
    }
}
