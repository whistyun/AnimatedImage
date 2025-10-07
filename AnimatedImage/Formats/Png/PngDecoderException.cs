using System;

namespace AnimatedImage.Formats.Png
{
    [Serializable]
    public class PngDecoderException : Exception
    {
        internal PngDecoderException() { }
        internal PngDecoderException(string message) : base(message) { }
        internal PngDecoderException(string message, Exception inner) : base(message, inner) { }
    }
}
