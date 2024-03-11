using System.IO;

namespace AnimatedImage.Avalonia
{
    internal static class StreamExt
    {
        public static Stream SupportSeek(this Stream strm)
        {
            if (strm.CanSeek)
                return strm;

            var memstream = new MemoryStream();
            strm.CopyTo(memstream);
            return memstream;
        }
    }
}
