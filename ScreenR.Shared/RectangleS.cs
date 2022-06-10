using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RectangleS
    {
        public RectangleS(int left, int top, int width, int height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public int Left;
        public int Top;
        public int Width;
        public int Height;
    }
}
