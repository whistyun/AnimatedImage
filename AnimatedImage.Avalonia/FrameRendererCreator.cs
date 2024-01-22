using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;

using System.Text;
using System.Threading.Tasks;

namespace AnimatedImage.Avalonia
{
    internal class FrameRendererCreator
    {

#if !NETFRAMEWORK
        private static readonly System.Net.Http.HttpClient s_client = new();
#endif

        public static bool TryCreate(
            IBitmapSource image,
#if !NETFRAMEWORK
            [MaybeNullWhen(false)]
#endif
            out FrameRenderer renderer)
        {
            if (image is BitmapStream bmp && bmp.StreamSource != null)
                return FrameRenderer.TryCreate(bmp.StreamSource, new WriteableBitmapFaceFactory(), out renderer);

            if (image is BitmapUri bur && bur.UriSource != null)
                if (TryOpen(bur.UriSource, out var stream))
                    return FrameRenderer.TryCreate(stream, new WriteableBitmapFaceFactory(), out renderer);

            renderer = null!;
            return false;
        }

        private static bool TryOpen(
            Uri resourceUri,
#if !NETFRAMEWORK
            [MaybeNullWhen(false)]
#endif
            out Stream strm
            )
        {
            var stream = OpenFirst(resourceUri);
            if (stream is null)
            {
                strm = null!;
                return false;
            }

            if (stream.CanSeek)
            {
                strm = stream;
                return true;
            }

            var memstream = new MemoryStream();
            stream.CopyTo(memstream);
            strm = memstream;
            return true;


            static Stream? OpenFirst(Uri uri)
            {
                switch (uri.Scheme)
                {
                    case "avares":
                        return AssetLoader.Open(uri);

#if NETFRAMEWORK
                    case "http":
                    case "https":
                    case "file":
                    case "ftp":
                        var wc = new WebClient();
                        return wc.OpenRead(uri);
#else
                    case "http":
                    case "https":
                        return s_client.GetStreamAsync(uri).Result;

                    case "file":
                        return File.OpenRead(uri.LocalPath);
#endif
                }

                return null;
            }
        }
    }
}