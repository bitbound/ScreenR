using SkiaSharp;
using System.Drawing;

namespace ScreenR.Core.Extensions
{
    public static class BitmapExtensions
    {
        public static SKRect ToRectangle(this SKBitmap self)
        {
            return new SKRect(0, 0, self.Width, self.Height);
        }
    }
}
