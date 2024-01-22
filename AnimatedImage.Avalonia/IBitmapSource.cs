using System;
using System.IO;

namespace AnimatedImage.Avalonia
{
    public interface IBitmapSource
    {
    }

    public record BitmapStream : IBitmapSource
    {
        public Stream StreamSource
        {
            get;
#if NET5_0_OR_GREATER
            init;
#endif
        }

#if NET5_0_OR_GREATER
        public BitmapStream() { }
#endif

        public BitmapStream(Stream stream)
        {
            StreamSource = stream;
        }

        public static implicit operator BitmapStream(Stream stream)
            => new BitmapStream(stream);
    }

    public record BitmapUri : IBitmapSource
    {
        public Uri UriSource
        {
            get;
#if NET5_0_OR_GREATER
            init;
#endif
        }

#if NET5_0_OR_GREATER
        public BitmapUri() { }
#endif

        public BitmapUri(Uri uri)
        {
            UriSource = uri;
        }

        public static implicit operator BitmapUri(Uri uri)
            => new BitmapUri(uri);
    }
}
