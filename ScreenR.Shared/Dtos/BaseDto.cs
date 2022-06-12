using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Dtos
{
    [DataContract]
    public abstract class BaseDto
    {
        public virtual DtoType DtoType { get; init; }
    }
}
