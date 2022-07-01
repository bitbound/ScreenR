using ScreenR.Shared.Enums;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace ScreenR.Shared.Models
{
    [DataContract]
    public class Device
    {
        [DataMember]
        public string Alias { get; init; } = string.Empty;

        [DataMember]
        public Architecture Architecture { get; init; }

        [DataMember]
        public string Name { get; init; } = string.Empty;


        [DataMember]
        public bool IsOnline { get; set; }

        [DataMember]
        public DateTimeOffset LastOnline { get; set; }

        [DataMember]
        public string OperatingSystem { get; init; } = string.Empty;

        [DataMember]
        public Platform Platform { get; init; }

        [DataMember]
        public int ProcessorCount { get; init; }

        [DataMember]
        public double TotalMemory { get; init; }

        [DataMember]
        public double TotalStorage { get; init; }

        [DataMember]
        public ConnectionType Type { get; init; }

        [DataMember]
        public double UsedMemory { get; init; }

        [DataMember]
        public double UsedStorage { get; init; }
    }
}
