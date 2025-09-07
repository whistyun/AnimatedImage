using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using AnimatedImage.Formats;
using AnimatedImage.Formats.Gif;
using AnimatedImage.Formats.Png;

namespace AnimatedImage
{
    /// <summary>
    /// Rendering helper for one image source.
    /// </summary>
    public abstract class FrameRenderer : IDisposable
    {
        private bool disposedValue;

        /// <summary>
        /// Currently drawn frame index.
        /// </summary>
        public abstract int CurrentIndex { get; }

        /// <summary>
        /// Total frame count.
        /// </summary>
        public abstract int FrameCount { get; }

        /// <summary>
        /// How many repeat animation loop.
        /// This value is got from the image source.
        /// </summary>
        public abstract int RepeatCount { get; }

        /// <summary>
        /// The duration of one loop of animation.
        /// </summary>
        public abstract TimeSpan Duration { get; }

        /// <summary>
        /// The image whitch the current frame was drawn.
        /// </summary>
        public abstract IBitmapFace Current { get; }

        /// <summary>
        /// Accesses a frame information
        /// </summary>
        /// <param name="frameIndex">The frame index</param>
        /// <returns></returns>
        public abstract FrameRenderFrame this[int frameIndex] { get; }

        /// <summary>
        /// Computes target frame to be drawn from elapsed time, and draws it.
        /// </summary>
        /// <param name="timespan">The elapsed time. It may be larger than <see cref="Duration"/>.</param>
        public void ProcessFrame(TimeSpan timespan)
        {
            while (timespan > Duration)
            {
                timespan -= Duration;
            }

            for (var frameIdx = 0; frameIdx < FrameCount; ++frameIdx)
            {
                var frame = this[frameIdx];
                if (frame.Begin <= timespan && timespan < frame.End)
                {
                    ProcessFrame(frameIdx);
                    return;
                }
            }
        }

        /// <summary>
        /// Draws the frame indicated by index.
        /// </summary>
        /// <param name="frameIndex">The target frame index.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// frameindex is less than 0, or larger than or equals to the count of frames.
        /// </exception>
        public abstract void ProcessFrame(int frameIndex);

        public abstract Task ProcessFrameAsync(int frameIndex);

        /// <summary>
        /// Retrieves the elapsed time until the specified frame is drawn. If the first frame is specified, returns 0 seconds.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public abstract TimeSpan GetStartTime(int idx);

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>the cloned instance.</returns>
        public abstract FrameRenderer Clone();

        /// <summary>
        /// Parses the binary stream and creats a FrameRenderer instance.
        /// </summary>
        /// <param name="stream">File streams, response data, etc.</param>
        /// <param name="factory">A wrapper for image creation and image rendering.</param>
        /// <param name="renderer">created instance.</param>
        /// <returns>Returns true if a binary stream is supported.</returns>
        public static bool TryCreate(
            Stream stream,
            IBitmapFaceFactory factory,
#if !NETFRAMEWORK
            [MaybeNullWhen(false)]
#endif
            out FrameRenderer renderer)
        {
            stream.Position = 0;
            var magic = new byte[Signature.MaxLength];
            stream.Read(magic, 0, magic.Length);

            stream.Position = 0;
            if (Signature.IsGifSignature(magic))
            {
                var gif = new GifFile(stream);
                renderer = new GifRenderer(gif, factory);
                return true;
            }

            if (Signature.IsPngSignature(magic))
            {
                var png = new ApngFile(stream);

                renderer = new PngRenderer(png, factory);
                return true;
            }

            if (WebpRenderer.CheckSupport() && Signature.IsWebPSignature(magic))
            {
                renderer = new WebpRenderer(stream, factory);
                return true;
            }

            renderer = null!;
            return false;
        }


        private static class Signature
        {
            public static readonly byte[] GifHead = new byte[] { 0x47, 0x49, 0x46, 0x38 }; // GIF8
            public static readonly byte[] Png = ApngFrame.Signature;
            public static readonly byte[] WebpHead = new byte[] { 0x52, 0x49, 0x46, 0x46 }; // RIFF
            public static readonly byte[] WebpHead2 = new byte[] { 0x57, 0x45, 0x42, 0x50 }; // WEBP
            public static readonly int MaxLength = Math.Max(Math.Max(GifHead.Length + 2, Png.Length), WebpHead.Length + 4 + WebpHead2.Length);

            public static bool IsGifSignature(byte[] signature)
            {
                if (signature.Length < 6)
                    return false;

                // GIF8
                for (var i = 0; i < GifHead.Length; ++i)
                    if (signature[i] != GifHead[i])
                        return false;

                // 8 or 9
                if (signature[4] != '8' && signature[4] != '9')
                    return false;

                // a
                if (signature[5] != 'a')
                    return false;

                return true;
            }

            public static bool IsPngSignature(byte[] signature)
            {
                if (signature.Length < Png.Length)
                    return false;

                for (var i = 0; i < Png.Length; ++i)
                    if (signature[i] != Png[i])
                        return false;

                return true;
            }

            public static bool IsWebPSignature(byte[] signature)
            {
                if (signature.Length < 12)
                    return false;

                // WebP signature
                // "RIFF" [4 bytes chunksize] "WEBP"

                for (var i = 0; i < WebpHead.Length; ++i)
                    if (signature[i] != WebpHead[i])
                        return false;

                for (var i = 8; i < WebpHead2.Length; ++i)
                    if (signature[i] != WebpHead2[i])
                        return false;

                return true;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: Discard managed state (managed object)
                }

                // TODO: Release unmanaged resources (unmanaged objects) and override finalizers.
                // TODO: Set large fields to null.
                disposedValue = true;
            }
        }

        // // TODO: Only override the finalizer if the ‘Dispose(bool disposing)’ contains code that releases unmanaged resources.
        // ~FrameRenderer()
        // {
        //     // Do not modify this code. Write cleanup code in the ‘Dispose(bool disposing)’ method.
        //     Dispose(disposing: false);
        // }

        void IDisposable.Dispose()
        {
            // Do not modify this code. Write cleanup code in the ‘Dispose(bool disposing)’ method.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
