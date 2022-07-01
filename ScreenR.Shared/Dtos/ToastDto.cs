using ScreenR.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Dtos
{
    [DataContract]
    public class ToastDto
    {
        [DataMember]
        public string Message { get; set; } = string.Empty;

        [DataMember]
        public MessageLevel MessageLevel { get; set; }
    }
}
