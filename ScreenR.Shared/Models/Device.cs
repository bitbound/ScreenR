using ScreenR.Shared.Enums;
using ScreenR.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Models
{
    [DataContract]
    public class Device
    {
        [DataMember]
        public Architecture Architecture { get; init; }

        [DataMember]
        public string ComputerName { get; init; } = string.Empty;

        [DataMember]
        public bool Is64Bit { get; init; }

        [DataMember]
        public bool IsOnline { get; set; }

        [DataMember]
        public string OperatingSystem { get; init; } = string.Empty;

        [DataMember]
        public Platform Platform { get; init; }

        [DataMember]
        public int ProcessorCount { get; init; }

        [DataMember]
        public ConnectionType Type { get; init; }

        [DataMember]
        public DateTimeOffset LastOnline { get; internal set; }

        public static ServiceDevice CreateService(
            Guid deviceId,
            bool isOnline)
        {
            return new ServiceDevice()
            {
                Type = ConnectionType.Service,
                DeviceId = deviceId,
                IsOnline = isOnline,
                Architecture = RuntimeInformation.OSArchitecture,
                ComputerName = Environment.MachineName,
                Is64Bit = Environment.Is64BitOperatingSystem,
                OperatingSystem = RuntimeInformation.OSDescription,
                Platform = EnvironmentHelper.Platform,
                ProcessorCount = Environment.ProcessorCount
            };
        }

        public static DesktopDevice CreateDesktop(
            Guid sessionId,
            bool isOnline)
        {
            return new DesktopDevice()
            {
                Type = ConnectionType.Desktop,
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
