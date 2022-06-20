using ScreenR.Desktop.Shared.Dtos;
using ScreenR.Desktop.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Core.Interfaces
{
    public interface IDesktopStreamer
    {
        IAsyncEnumerable<DesktopFrameChunk> GetDesktopStream(CancellationToken cancellationToken = default);
        void SetActiveDisplay(string deviceName);
        IEnumerable<DisplayInfo> GetDisplays();
    }
}
