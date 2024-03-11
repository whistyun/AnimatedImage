using System;
using System.ComponentModel;
using System.IO;

namespace AnimatedImage.Avalonia
{
    /// <summary>
    /// Represents an object type that creates FrameRenderer.
    /// </summary>
    [TypeConverter(typeof(AnimatedImageSourceConverter))]
    public abstract record AnimatedImageSource
    {
        /// <summary>
        /// Creates FrameRenderer
        /// </summary>
        /// <returns>The created FrameRenderer, or null if creation is failed.</returns>
        public abstract FrameRenderer? TryCreate();

        /// <summary>
        /// Opens the uri and creates AnimatedImageSource.
        /// </summary>
        /// <param name="uri">The uri indentifying an image resource.</param>
        public static implicit operator AnimatedImageSource(Uri uri) => new AnimatedImageSourceUri(uri);
    }
}
