using SkiaSharp;
using System.Drawing;

namespace ScreenR.Core.Extensions
{
    public static class BitmapExtensions
    {
        public static Rectangle ToRectangle(this SKBitmap self)
        {
            return new Rectangle(0, 0, self.Width, self.Height);
        }
    }
}
