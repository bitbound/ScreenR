using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Models
{
    [DataContract]
    public class ServiceDevice : Device
    {
        [DataMember]
        public int Id { get; internal set; }

        [DataMember]
        public Guid DeviceId { get; init; }
    }
}
