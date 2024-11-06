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

        public override Stream? SourceSeekable
            => StreamSource is not null ? StreamSource.SupportSeek() : null;

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
            if (StreamSource is null)
                return null;

            var strm = StreamSource.SupportSeek();
            if (FrameRenderer.TryCreate(strm, new WriteableBitmapFaceFactory(), out var renderer))
                return renderer;

            return null;
        }
    }
}
