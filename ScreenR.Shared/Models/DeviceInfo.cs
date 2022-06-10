using ScreenR.Shared.Enums;
using ScreenR.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Models
{
    [DataContract]
    public class DeviceInfo
    {
        public static DeviceInfo Empty { get; } = new DeviceInfo();

        [DataMember]
        public Architecture Architecture { get; init; }

        [DataMember]
        public string ComputerName { get; init; } = string.Empty;

        [DataMember]
        public Guid DeviceId { get; init; }

        [DataMember]
        public bool Is64Bit { get; init; }

        [DataMember]
        public bool IsOnline { get; init; } = true;

        [DataMember]
        public string OperatingSystem { get; init; } = string.Empty;

        [DataMember]
        public Platform Platform { get; init; }

        [DataMember]
        public int ProcessorCount { get; init; }

        [DataMember]
        public Guid SessionId { get; init; }

        [DataMember]
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
