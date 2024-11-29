using AnimatedImage.Formats.Png.Types;
using AnimatedImage.Formats.Png;
using System;
using System.Threading.Tasks;

namespace AnimatedImage.Formats
{
    internal abstract class PngRendererFrame : FrameRenderFrame
    {
        public DisposeOps DisposalMethod { get; }
        public BlendOps BlendMethod { get; }

        protected PngRendererFrame(ApngFile file, Png.ApngFrame frame, TimeSpan begin) :
            base(
                (int)Nvl(frame.fcTLChunk?.XOffset, 0u),
                (int)Nvl(frame.fcTLChunk?.YOffset, 0u),
                (int)Nvl(frame.fcTLChunk?.Width, (uint)file.IHDRChunk.Width),
                (int)Nvl(frame.fcTLChunk?.Height, (uint)file.IHDRChunk.Height),
                begin,
                begin + Nvl(frame.fcTLChunk?.ComputeDelay(), TimeSpan.FromMilliseconds(100)))
        {
            if (frame.fcTLChunk is null)
            {
                DisposalMethod = DisposeOps.APNGDisposeOpNone;
                BlendMethod = BlendOps.APNGBlendOpSource;
            }
            else
            {
                DisposalMethod = frame.fcTLChunk.DisposeOp;
                BlendMethod = frame.fcTLChunk.BlendOp;
            }
        }

        public abstract void Render(IBitmapFace bitmap, byte[] work, byte[]? backup);
        public abstract Task RenderAsync(IBitmapFace bitmap, byte[] work, byte[]? backup);


        private static T Nvl<T>(T? v1, T v2) where T : struct => v1.HasValue ? v1.Value : v2;
    }
}
