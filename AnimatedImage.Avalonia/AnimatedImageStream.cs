using System;
using System.IO;

namespace AnimatedImage.Avalonia
{
    /// <summary>
    /// Represents an object type that creates FrameRenderer from stream.
    /// </summary>
    public class AnimatedImageStream : AnimatedImageSource
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

#if NET5_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public AnimatedImageStream() { }
#endif
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="stream">The image stream.</param>
        public AnimatedImageStream(Stream stream)
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
