using ScreenR.Shared.Enums;
using ScreenR.Shared.Models;
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
            return new DesktopDevice()
            {
                Type = ConnectionType.Desktop,
                SessionId = sessionId,
                IsOnline = isOnline,
                UsedStorage = GetUsedStorage(),
                TotalStorage = GetTotalStorage(),
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
            return new ServiceDevice()
            {
                Type = ConnectionType.Service,
                DeviceId = deviceId,
                IsOnline = isOnline,
                UsedStorage = GetUsedStorage(),
                TotalStorage = GetTotalStorage(),
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
                _systemDrive = OperatingSystem.IsWindows() ?
                    DriveInfo.GetDrives().FirstOrDefault(x => x.IsReady && x.RootDirectory.FullName.Contains(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\")) :
                    DriveInfo.GetDrives().FirstOrDefault(x => x.IsReady && x.RootDirectory.FullName == Path.GetPathRoot(Environment.CurrentDirectory));
            }

            if (_systemDrive is null)
            {
                return Result.Fail<DriveInfo>("System drive not found.");
            }

            return Result.Ok(_systemDrive);
        }
    }
}
