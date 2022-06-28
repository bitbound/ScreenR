using ScreenR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Dtos
{
    [DataContract]
    public class WindowsSessions : BaseDto
    {
        [DataMember]
        public IEnumerable<WindowsSession> Sessions { get; init; } = Enumerable.Empty<WindowsSession>();

        [DataMember]
        public override DtoType DtoType { get; init; } = DtoType.WindowsSessions;
    }
}
