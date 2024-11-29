using System.Runtime.InteropServices;

namespace AnimatedImage.Formats.Gif
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GifColor
    {
        public byte B { get; }
        public byte G { get; }
        public byte R { get; }
        public byte A { get; } = 255;

        internal GifColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public override string ToString()
        {
            return string.Format("#{0:x2}{1:x2}{2:x2}", R, G, B);
        }
    }
}
