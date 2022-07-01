using ScreenR.Desktop.Shared.Native.Windows;
using ScreenR.Shared;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Helpers;
using ScreenR.Shared.Models;
using System.Runtime.InteropServices;

namespace ScreenR.Desktop.Shared.Services
{
    public interface IDeviceCreator
    {
        DesktopDevice CreateDesktop(Guid sessionId, bool isOnline);
        ServiceDevice CreateService(Guid deviceId, bool isOnline);
    }

    internal class DeviceCreator : IDeviceCreator
    {
        private readonly IProcessLauncher _processLauncher;
        private DriveInfo? _systemDrive;

        public DeviceCreator(IProcessLauncher processLauncher)
        {
            _processLauncher = processLauncher;
        }

        public DesktopDevice CreateDesktop(
            Guid sessionId,
            bool isOnline)
        {
            var totalMemory = GetTotalMemory();

            return new DesktopDevice()
            {
                Type = ConnectionType.Desktop,
                SessionId = sessionId,
                IsOnline = isOnline,
                UsedStorage = GetUsedStorage(),
                TotalStorage = GetTotalStorage(),
                UsedMemory = GetUsedMemory(totalMemory),
                TotalMemory = totalMemory,
                Architecture = RuntimeInformation.OSArchitecture,
                Name = Environment.MachineName,
                OperatingSystem = RuntimeInformation.OSDescription,
                Platform = EnvironmentHelper.Platform,
                ProcessorCount = Environment.ProcessorCount
            };
        }

        public ServiceDevice CreateService(
            Guid deviceId,
            bool isOnline)
        {
            var totalMemory = GetTotalMemory();

            return new ServiceDevice()
            {
                Type = ConnectionType.Service,
                DeviceId = deviceId,
                IsOnline = isOnline,
                UsedStorage = GetUsedStorage(),
                TotalStorage = GetTotalStorage(),
                UsedMemory = GetUsedMemory(totalMemory),
                TotalMemory = totalMemory,
                Architecture = RuntimeInformation.OSArchitecture,
                Name = Environment.MachineName,
                OperatingSystem = RuntimeInformation.OSDescription,
                Platform = EnvironmentHelper.Platform,
                ProcessorCount = Environment.ProcessorCount
            };
        }

        private double GetMemInfoRow(string rowTitle)
        {
            var result = _processLauncher.GetProcessOutput("cat", "/proc/meminfo").GetAwaiter().GetResult();
            if (!result.IsSuccess || result.Value is null)
            {
                return 0;
            }

            var resultsArr = result.Value.Split("\n");
            if (resultsArr is null)
            {
                return 0;
            }

            var totalKbString = resultsArr
                .FirstOrDefault(x => x.Trim().StartsWith(rowTitle))
                ?.Trim()
                ?.Split(" ".ToCharArray(), 2)
                ?.Last()
                ?.Trim()
                ?.Split(' ')
                ?.First();

            if (!double.TryParse(totalKbString, out var totalKb))
            {
                return 0;
            }
            return Math.Round(totalKb / 1024 / 1024, 2);
        }

        private double GetTotalMemory()
        {
            try
            {
                switch (EnvironmentHelper.Platform)
                {
                    case Platform.Windows:
                        {
                            var memoryStatus = new MemoryStatusEx();

                            if (Kernel32Ex.GlobalMemoryStatusEx(memoryStatus))
                            {
                                return Math.Round((double)memoryStatus.ullTotalPhys / 1024 / 1024 / 1024, 2);
                            }
                        }
                        break;
                    case Platform.Linux:
                        return GetMemInfoRow("MemTotal");
                    case Platform.MacOS:
                    case Platform.Unknown:
                    case Platform.MacCatalyst:
                    default:
                        return 0;
                }

            }
            catch { }
            return 0;
        }

        private double GetTotalStorage()
        {
            var result = TryGetSystemDrive();

            if (!result.IsSuccess || result.Value is null)
            {
                return 0;
            }
            // Storage in GB.
            return Math.Round((double)(result.Value.TotalSize / 1024 / 1024 / 1024), 2);
        }

        private double GetUsedMemory(double totalGb)
        {
            try
            {
                switch (EnvironmentHelper.Platform)
                {
                    case Platform.Windows:
                        {
                            var memoryStatus = new MemoryStatusEx();
                            if (Kernel32Ex.GlobalMemoryStatusEx(memoryStatus))
                            {
                                var freeGB = Math.Round((double)memoryStatus.ullAvailPhys / 1024 / 1024 / 1024, 2);
                                return totalGb - freeGB;
                            }
                        }
                        break;
                    case Platform.Linux:
                        return totalGb - GetMemInfoRow("MemAvailable");
                    case Platform.MacOS:
                    case Platform.Unknown:
                    case Platform.MacCatalyst:
                    default:
                        return 0;
                }
            }
            catch { }
            return 0;
        }

        private double GetUsedStorage()
        {
            var result = TryGetSystemDrive();

            if (!result.IsSuccess || result.Value is null)
            {
                return 0;
            }

            // Storage in GB.
            return Math.Round((double)((result.Value.TotalSize - result.Value.TotalFreeSpace) / 1024 / 1024 / 1024), 2);
        }

        private Result<DriveInfo> TryGetSystemDrive()
        {
            if (_systemDrive is null)
            {
                _systemDrive = EnvironmentHelper.Platform switch
                {
                    Platform.Windows => DriveInfo.GetDrives().FirstOrDefault(x =>
                        x.IsReady &&
                        x.RootDirectory.FullName.Contains(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\")),
                    Platform.Linux => DriveInfo.GetDrives().FirstOrDefault(x =>
                        x.IsReady &&
                        x.RootDirectory.FullName == (Path.GetPathRoot(Environment.CurrentDirectory) ?? "/")),
                    _ => null
                };
            }

            if (_systemDrive is null)
            {
                return Result.Fail<DriveInfo>("System drive not found.");
            }

            return Result.Ok(_systemDrive);
        }
    }
}
