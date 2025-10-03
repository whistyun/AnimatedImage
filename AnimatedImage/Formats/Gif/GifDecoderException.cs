using System;

namespace AnimatedImage.Formats.Gif
{
    [Serializable]
    internal class GifDecoderException : Exception
    {
        internal GifDecoderException() { }
        internal GifDecoderException(string message) : base(message) { }
        internal GifDecoderException(string message, Exception inner) : base(message, inner) { }
    }
}
