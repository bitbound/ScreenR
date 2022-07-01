using ScreenR.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Control.Models
{
    public struct SentFrame
    {
        public SentFrame(long frameSize)
        {
            Timestamp = Time.Now;
            FrameSize = frameSize;
        }

        public DateTimeOffset Timestamp { get; }
        public long FrameSize { get; }
    }
}
