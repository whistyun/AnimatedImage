using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AnimatedImage.Wpf
{
    internal class WriteableBitmapFace : IBitmapFace
    {
        public WriteableBitmap Bitmap { get; }

        public WriteableBitmapFace(int width, int height)
        {
            Bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        }

        public void ReadBGRA(byte[] buffer, int x, int y, int width, int height)
        {
            var bounds = new Int32Rect(x, y, Math.Min(width, Bitmap.PixelWidth - x), Math.Min(height, Bitmap.PixelHeight - y));
            Bitmap.CopyPixels(bounds, buffer, 4 * width, 0);
        }

        public void WriteBGRA(byte[] buffer, int x, int y, int width, int height)
        {
            var bounds = new Int32Rect(x, y, Math.Min(width, Bitmap.PixelWidth - x), Math.Min(height, Bitmap.PixelHeight - y));
            Bitmap.WritePixels(bounds, buffer, 4 * width, 0);
        }

        public unsafe void Clear(int x, int y, int width, int height)
        {
            Bitmap.Lock();

            if (x < 0 || x + width > Bitmap.Width)
                throw new ArgumentException();

            if (y < 0 || y + height > Bitmap.Height)
                throw new ArgumentException();

            var leftTop = Bitmap.BackBuffer + (y * Bitmap.BackBufferStride) + (x * 4);
            var lineLength = (uint)width * 4;
            for (var i = 0; i < height; ++i)
            {
                Unsafe.InitBlock(leftTop.ToPointer(), 0, lineLength);
                leftTop += Bitmap.BackBufferStride;
            }

            Bitmap.AddDirtyRect(new Int32Rect(x, y, width, height));
            Bitmap.Unlock();
        }
    }

    internal class WriteableBitmapFaceFactory : IBitmapFaceFactory
    {
        public IBitmapFace Create(int width, int height)
            => new WriteableBitmapFace(width, height);
    }
}
