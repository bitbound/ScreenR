using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Native.Windows
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
        [DataMember(Name = "ID")]
        public int ID { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [DataMember(Name = "Type")]
        public SessionType Type { get; set; }

        [DataMember(Name = "Username")]
        public string Username { get; set; } = string.Empty;
    }
}
