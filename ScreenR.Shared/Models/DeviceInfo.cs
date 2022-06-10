using ScreenR.Shared.Enums;
using ScreenR.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Models
{
    public class DeviceInfo
    {
        public static DeviceInfo Empty { get; } = new DeviceInfo();

        public Architecture Architecture { get; init; }

        public string ComputerName { get; init; } = string.Empty;

        public Guid DeviceId { get; init; }

        public bool Is64Bit { get; init; }

        public bool IsOnline { get; init; } = true;

        public string OperatingSystem { get; init; } = string.Empty;

        public Platform Platform { get; init; }

        public int ProcessorCount { get; init; }

        public Guid SessionId { get; init; }

        public ConnectionType Type { get; init; }


        public static DeviceInfo Create(
            ConnectionType type,
            bool isOnline,
            Guid deviceId,
            Guid sessionId)
        {
            return new DeviceInfo()
            {
                Type = type,
                DeviceId = deviceId,
                SessionId = sessionId,
                IsOnline = isOnline,
                Architecture = RuntimeInformation.OSArchitecture,
                ComputerName = Environment.MachineName,
                Is64Bit = Environment.Is64BitOperatingSystem,
                OperatingSystem = RuntimeInformation.OSDescription,
                Platform = EnvironmentHelper.Platform,
                ProcessorCount = Environment.ProcessorCount
            };
        }
    }
}
