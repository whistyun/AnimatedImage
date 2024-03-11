using Avalonia.Platform;
using System;
using System.IO;

namespace AnimatedImage.Avalonia
{
    /// <summary>
    /// Represents an object type that creates FrameRenderer from uri.
    /// </summary>
    public class AnimatedImageUri : AnimatedImageSource
    {
#if !NETFRAMEWORK
        private static readonly System.Net.Http.HttpClient s_client = new();
#endif

        /// <summary>
        /// The uri indentifying an image resource.
        /// </summary>
        public Uri? UriSource
        {
            get;
#if NET5_0_OR_GREATER
            init;
#endif
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public AnimatedImageUri() { }
#endif
        /// <summary>
        /// Initializes a new instance
        /// </summary>
        /// <param name="uri">The uri indentifying an image resource.</param>
        public AnimatedImageUri(Uri uri)
        {
            UriSource = uri;
        }

        /// <inheritdoc/>
        public override FrameRenderer? TryCreate()
        {
            if (UriSource is null)
                return null;

            if (OpenFirst(UriSource) is { } stream)
            {
                var seekableStream = stream.SupportSeek();
                var factory = new WriteableBitmapFaceFactory();
                if (FrameRenderer.TryCreate(seekableStream, factory, out var renderer))
                    return renderer;
            }

            return null;
        }

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
                        var wc = new System.Net.WebClient();
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
