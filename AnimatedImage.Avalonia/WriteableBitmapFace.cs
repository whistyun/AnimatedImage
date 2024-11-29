using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AnimatedImage.Avalonia
{
    internal class WriteableBitmapFace : IBitmapFace
    {
        public IImage Bitmap
        {
            get
            {
                Send();
                return _bitmapWrapper;
            }
        }

        private readonly PixelSize _size;
        private readonly WriteableBitmap _buffer;
        private readonly WriteableBitmap _bitmap;
        public readonly IImage _bitmapWrapper;

        public WriteableBitmapFace(int width, int height)
        {
            _size = new PixelSize(width, height);
            var dpi = new Vector(96, 96);
            _buffer = new WriteableBitmap(_size, dpi, PixelFormats.Bgra8888, null);

            _bitmap = new WriteableBitmap(_size, dpi, PixelFormats.Bgra8888, null);
            _bitmapWrapper = new BitmapWrapper(_bitmap);
        }

        public void ReadBGRA(byte[] buffer, int x, int y, int width, int height)
        {
            if (width * height * 4 > buffer.Length)
                throw new IndexOutOfRangeException();

            using var bit = _buffer.Lock();

            IntPtr ptr = bit.Address;
            ptr += y * bit.RowBytes + x * 4;

            var bufferOffset = 0;
            var copyLen = Math.Min(_size.Width - x, width) * 4;

            for (var i = 0; i < Math.Min(_size.Height - y, height); ++i)
            {
                Marshal.Copy(ptr, buffer, bufferOffset, copyLen);
                bufferOffset += width * 4;
                ptr += bit.RowBytes;
            }
        }

        public void WriteBGRA(byte[] buffer, int x, int y, int width, int height)
        {
            if (width * height * 4 > buffer.Length)
                throw new IndexOutOfRangeException();

            using var bit = _buffer.Lock();

            IntPtr ptr = bit.Address;
            ptr += y * bit.RowBytes + x * 4;

            var bufferOffset = 0;
            var copyLen = Math.Min(_size.Width - x, width) * 4;

            for (var i = 0; i < Math.Min(_size.Height - y, height); ++i)
            {
                Marshal.Copy(buffer, bufferOffset, ptr, copyLen);
                bufferOffset += width * 4;
                ptr += bit.RowBytes;
            }
        }

        public unsafe void Clear(int x, int y, int width, int height)
        {
            using var bit = _buffer.Lock();

            IntPtr ptr = bit.Address;
            ptr += y * bit.RowBytes + x * 4;

            var copyLen = (uint)Math.Min(_size.Width - x, width) * 4;
            for (var i = 0; i < Math.Min(_size.Height - y, height); ++i)
            {
                Unsafe.InitBlock(ptr.ToPointer(), 0, copyLen);
                ptr += bit.RowBytes;
            }
        }

        private void Send()
        {
            using var fBit = _bitmap.Lock();
            _buffer.CopyPixels(
                new PixelRect(_size),
                fBit.Address,
                fBit.RowBytes * _size.Height,
                fBit.RowBytes);
        }

        class BitmapWrapper : IImage
        {
            private IImage _bitmap;

            public BitmapWrapper(WriteableBitmap bitmap)
            {
                _bitmap = bitmap;
            }

            public Size Size
                => _bitmap.Size;

            public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
                => _bitmap.Draw(context, sourceRect, destRect);
        }
    }

    internal class WriteableBitmapFaceFactory : IBitmapFaceFactory
    {
        public IBitmapFace Create(int width, int height) => new WriteableBitmapFace(width, height);
    }
}
