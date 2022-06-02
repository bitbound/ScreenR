using System.Drawing;

namespace ScreenR.Core.Models
{
    public class DisplayInfo
    {
        public int BitsPerPixel { get; set; }
        public Rectangle Bounds { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }
}
