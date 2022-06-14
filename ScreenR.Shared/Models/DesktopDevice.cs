using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Models
{
    public class DesktopDevice : Device
    {
        [DataMember]
        public Guid SessionId { get; init; }
    }
}
