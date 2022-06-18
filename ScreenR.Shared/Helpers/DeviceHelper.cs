using ScreenR.Shared.Enums;
using ScreenR.Shared.Models;
using ScreenR.Shared.Native.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Helpers
{
    internal class DeviceHelper
    {
        private static DriveInfo? _systemDrive;

        public static DesktopDevice CreateDesktop(
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
                ComputerName = Environment.MachineName,
                OperatingSystem = RuntimeInformation.OSDescription,
                Platform = EnvironmentHelper.Platform,
                ProcessorCount = Environment.ProcessorCount
            };
        }

        public static ServiceDevice CreateService(
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
                ComputerName = Environment.MachineName,
                OperatingSystem = RuntimeInformation.OSDescription,
                Platform = EnvironmentHelper.Platform,
                ProcessorCount = Environment.ProcessorCount
            };
        }

        public static string GetFormattedStoragePercent(Device device)
        {
            return $"{Math.Round(device.UsedStorage / device.TotalStorage * 100)}%";
        }

        public static string GetFormattedMemoryPercent(Device device)
        {
            return $"{Math.Round(device.UsedMemory / device.TotalMemory * 100)}%";
        }

        private static double GetMemInfoRow(string rowTitle)
        {
            var result = ProcessHelper.GetProcessOutput("cat", "/proc/meminfo").GetAwaiter().GetResult();
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

        private static double GetTotalMemory()
        {
            try
            {
                switch (EnvironmentHelper.Platform)
                {
                    case Platform.Windows:
                        {
                            var memoryStatus = new MemoryStatusEx();

                            if (Kernel32.GlobalMemoryStatusEx(memoryStatus))
                            {
                                return Math.Round(((double)memoryStatus.ullTotalPhys / 1024 / 1024 / 1024), 2);
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

        private static double GetTotalStorage()
        {
            var result = TryGetSystemDrive();

            if (!result.IsSuccess || result.Value is null)
            {
                return 0;
            }
            // Storage in GB.
            return Math.Round((double)(result.Value.TotalSize / 1024 / 1024 / 1024), 2);
        }

        private static double GetUsedMemory(double totalGb)
        {
            try
            {
                switch (EnvironmentHelper.Platform)
                {
                    case Platform.Windows:
                        {
                            var memoryStatus = new MemoryStatusEx();
                            if (Kernel32.GlobalMemoryStatusEx(memoryStatus))
                            {
                                var freeGB = Math.Round(((double)memoryStatus.ullAvailPhys / 1024 / 1024 / 1024), 2);
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

        private static double GetUsedStorage()
        {
            var result = TryGetSystemDrive();

            if (!result.IsSuccess || result.Value is null)
            {
                return 0;
            }

            // Storage in GB.
            return Math.Round((double)((result.Value.TotalSize - result.Value.TotalFreeSpace) / 1024 / 1024 / 1024), 2);
        }

        private static Result<DriveInfo> TryGetSystemDrive()
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
