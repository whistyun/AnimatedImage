using AnimatedImage;
using SkiaSharp;
using System;
using System.Runtime.InteropServices;

namespace AnimatedImageTest
{
    internal class BitmapFace : IBitmapFace
    {
        private int _width;
        private int _height;
        private SKBitmap _bitmap;

        public SKBitmap Bitmap => _bitmap;

        public BitmapFace(int w, int h)
        {
            _width = w;
            _height = h;
            _bitmap = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
        }

        public void Clear(int x, int y, int width, int height)
        {
            IntPtr pixels = _bitmap.GetPixels();
            int stride = _bitmap.RowBytes;

            var empties = new byte[width * 4];

            pixels += stride * y + 4 * x;
            for (int i=0; i < height; ++i)
            {
                Marshal.Copy(empties, 0, pixels, 4 * width);
                pixels += stride;
            }
        }

        public void ReadBGRA(byte[] buffer, int x, int y, int width, int height)
        {
            IntPtr pixels = _bitmap.GetPixels();
            int stride = _bitmap.RowBytes;

            pixels += stride * y + 4 * x;

            int offset = 0;
            for (int i = 0; i < height; ++i)
            {
                Marshal.Copy(pixels, buffer, offset, 4 * width);
                offset += 4 * width;
                pixels += stride;
            }
        }

        public void WriteBGRA(byte[] buffer, int x, int y, int width, int height)
        {
            IntPtr pixels = _bitmap.GetPixels();
            int stride = _bitmap.RowBytes;

            pixels += stride * y + 4 * x;

            int offset = 0;
            for (int i = 0; i < height; ++i)
            {
                Marshal.Copy(buffer, offset, pixels, 4 * width);
                offset += 4 * width;
                pixels += stride;
            }
        }
    }
}
