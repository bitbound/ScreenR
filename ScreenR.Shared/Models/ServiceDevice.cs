using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Models
{
    public class ServiceDevice : Device
    {
        [DataMember]
        [Key]
        public int Id { get; init; } = -1;

        [DataMember]
        public Guid DeviceId { get; init; }
    }
}
