using ScreenR.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Models
{
    [DataContract]
    public enum SessionType
    {
        [EnumMember]
        Unknown = 0,

        [EnumMember]
        Console = 1,

        [EnumMember]
        RDP = 2
    }

    [DataContract]
    public class WindowsSession
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string Name { get; set; } = string.Empty;

        [DataMember]
        public SessionType Type { get; set; }

        [DataMember]
        public string Username { get; set; } = string.Empty;
    }
}
