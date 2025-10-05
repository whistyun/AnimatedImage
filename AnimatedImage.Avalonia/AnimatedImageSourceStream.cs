using System;
using System.IO;

namespace AnimatedImage.Avalonia
{
    /// <summary>
    /// Represents an object type that creates FrameRenderer from stream.
    /// </summary>
    public record AnimatedImageSourceStream : AnimatedImageSource
    {
        /// <summary>
        /// The image stream.
        /// </summary>
        public Stream? StreamSource
        {
            get;
#if NET5_0_OR_GREATER
            init;
#endif
        }

        private Stream? _SourceSeekable;
        /// <inheritdoc/>
        public override Stream? SourceSeekable
        {
            get
            {
                if (StreamSource is null)
                    return null;

                if (_SourceSeekable is not null)
                {
                    _SourceSeekable.Position = 0;
                    return _SourceSeekable;
                }
                else
                {
                    return _SourceSeekable = StreamSource.SupportSeek();
                }
            }
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public AnimatedImageSourceStream() { }
#endif
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="stream">The image stream.</param>
        public AnimatedImageSourceStream(Stream stream)
        {
            StreamSource = stream;
        }

        /// <inheritdoc/>
        public override FrameRenderer? TryCreate()
        {
            if (SourceSeekable is { } strm
                && FrameRenderer.TryCreate(strm, new WriteableBitmapFaceFactory(), out var renderer))
                return renderer;

            return null;
        }
    }
}
