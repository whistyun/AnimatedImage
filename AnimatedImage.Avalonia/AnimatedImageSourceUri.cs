using Avalonia.Platform;
using System;
using System.IO;

namespace AnimatedImage.Avalonia
{
    /// <summary>
    /// Represents an object type that creates FrameRenderer from uri.
    /// </summary>
    public record AnimatedImageSourceUri : AnimatedImageSource
    {
        private bool _tried = false;
        private Stream? _SourceSeekable;

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

        /// <inheritdoc/>
        public override Stream? SourceSeekable
        {
            get
            {
                if (_tried)
                {
                    if (_SourceSeekable is not null)
                        _SourceSeekable.Position = 0;

                    return _SourceSeekable;
                }

                _tried = true;

                if (UriSource is null)
                    return null;

                var stream = StaticOpenFirst(UriSource);
                if (stream is null)
                    return null;

                return _SourceSeekable = stream.SupportSeek();
            }
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public AnimatedImageSourceUri() { }
#endif

        /// <summary>
        /// Initializes a new instance
        /// </summary>
        /// <param name="uri">The uri indentifying an image resource.</param>
        public AnimatedImageSourceUri(Uri uri)
        {
            UriSource = uri;
        }

        /// <inheritdoc/>
        public override FrameRenderer? TryCreate()
        {
            if (SourceSeekable is { } stream)
            {
                var factory = new WriteableBitmapFaceFactory();
                if (FrameRenderer.TryCreate(stream, factory, out var renderer))
                    return renderer;
            }

            return null;
        }

        static Stream? StaticOpenFirst(Uri uri)
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
