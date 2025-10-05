using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace AnimatedImage.Wpf
{
    internal class FrameRendererCreator
    {
#if !NETFRAMEWORK
        private static readonly System.Net.Http.HttpClient s_client = new();
#endif

        public static bool TryCreate(
            BitmapSource image,
            IUriContext context,
#if !NETFRAMEWORK
            [MaybeNullWhen(false)]
#endif
            out FrameRenderer renderer)
        {
            if (image is BitmapFrame frame)
            {
                if (Uri.TryCreate(frame.BaseUri, frame.ToString(), out var uri)
                    && TryOpen(uri, out var stream))
                {
                    using (stream)
                    {
                        return FrameRenderer.TryCreate(stream, new WriteableBitmapFaceFactory(), out renderer);
                    }
                }

                renderer = null!;
                return false;
            }

            if (image is BitmapImage bmp)
            {
                if (bmp.StreamSource != null)
                {
                    return FrameRenderer.TryCreate(bmp.StreamSource, new WriteableBitmapFaceFactory(), out renderer);
                }

                if (bmp.UriSource != null)
                {
                    var uri = bmp.UriSource;
                    if (!uri.IsAbsoluteUri)
                    {
                        var baseUri = bmp.BaseUri ?? context?.BaseUri;
                        if (baseUri != null)
                            uri = new Uri(baseUri, uri);
                    }

                    if (TryOpen(uri, out var stream))
                        using (stream)
                            return FrameRenderer.TryCreate(stream, new WriteableBitmapFaceFactory(), out renderer);
                }
            }

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
            else {
                strm = stream;
                return true;
            }

            static Stream? OpenFirst(Uri uri)
            {
                switch (uri.Scheme)
                {
                    case "pack":
                        StreamResourceInfo sri;
                        if (uri.Authority == "siteoforigin:,,,")
                            sri = Application.GetRemoteStream(uri);
                        else
                            sri = Application.GetResourceStream(uri);

                        if (sri != null)
                            return sri.Stream;
                        break;

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
