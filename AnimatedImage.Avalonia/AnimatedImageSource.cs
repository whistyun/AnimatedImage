using System;
using System.ComponentModel;
using System.IO;

namespace AnimatedImage.Avalonia
{
    /// <summary>
    /// Represents an object type that creates FrameRenderer.
    /// </summary>
    [TypeConverter(typeof(AnimatedImageSourceConverter))]
    public abstract class AnimatedImageSource
    {
        /// <summary>
        /// Creates FrameRenderer
        /// </summary>
        /// <returns>The created FrameRenderer, or null if creation is failed.</returns>
        public abstract FrameRenderer? TryCreate();

        /// <summary>
        /// Converts the string expressing uri to AnimatedImageSource.
        /// </summary>
        /// <param name="uriTxt">The string expressing uri.</param>
        public static implicit operator AnimatedImageSource(string uriTxt)
            => AnimatedImageSourceConverter.Convert(uriTxt);

        /// <summary>
        /// Opens the uri and creates AnimatedImageSource.
        /// </summary>
        /// <param name="uri">The uri indentifying an image resource.</param>
        public static implicit operator AnimatedImageSource(Uri uri) => new AnimatedImageUri(uri);

        /// <summary>
        /// Loads the stream and creates AnimatedImageSource.
        /// </summary>
        /// <param name="stream">The image stream.</param>
        public static implicit operator AnimatedImageSource(Stream stream) => new AnimatedImageStream(stream);
    }
}
