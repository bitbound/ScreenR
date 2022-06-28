using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Dtos
{
    [DataContract]
    public class BaseDto
    {
        [DataMember(Name = "DtoTypeBase")]
        public virtual DtoType DtoType { get; init; }

        [DataMember(Name = "RequestIdBase")]
        public virtual Guid RequestId { get; init; }
    }
}
