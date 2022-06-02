using ScreenR.Core.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Core.Interfaces
{
    public interface IScreenGrabber
    {
        IEnumerable<DisplayInfo> GetDisplays();

        Result<SKBitmap> GetScreenGrab(string outputName);
    }
}
