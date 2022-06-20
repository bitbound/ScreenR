using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Shared.Dtos
{
    [DataContract]
    public class DesktopFrameChunk : BaseDto
    {
        [DataMember]
        public Rectangle Area { get; init; }

        [DataMember]
        public override DtoType DtoType { get; init; } = DtoType.DesktopFrameChunk;

        [DataMember]
        public bool EndOfFrame { get; init; }
        [DataMember]
        public byte[] ImageBytes { get; init; } = Array.Empty<byte>();

    }
}
