using ScreenR.Desktop.Control.Models;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Control.Interfaces
{
    public interface IDesktopStreamer
    {
        event EventHandler? LoopIncremented;
        event EventHandler<SentFrame>? FrameSent;

        IAsyncEnumerable<DesktopFrameChunk> GetDesktopStream(CancellationToken cancellationToken);
        IEnumerable<DisplayInfo> GetDisplays();

        void SetActiveDisplay(string deviceName);
        void FrameReceived();
    }
}
