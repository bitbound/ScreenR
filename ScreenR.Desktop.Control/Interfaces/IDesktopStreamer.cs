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
        IAsyncEnumerable<DesktopFrameChunk> GetDesktopStream(CancellationToken cancellationToken = default);
        void SetActiveDisplay(string deviceName);
        IEnumerable<DisplayInfo> GetDisplays();
    }
}
