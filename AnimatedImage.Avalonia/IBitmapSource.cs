using System;
using System.IO;

namespace AnimatedImage.Avalonia
{
    public interface IBitmapSource
    {
    }

    public record BitmapStream : IBitmapSource
    {
        public Stream StreamSource { get; init; }

        public static implicit operator BitmapStream(Stream stream)
            => new BitmapStream { StreamSource = stream };
    }

    public record BitmapUri : IBitmapSource
    {
        public Uri UriSource { get; init; }

        public static implicit operator BitmapUri(Uri uri)
            => new BitmapUri { UriSource = uri };
    }
}
