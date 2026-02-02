using AnimatedImage.Formats.Png;
using AnimatedImage.Formats.Png.Chunks;
using AnimatedImage.Formats.Png.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace AnimatedImage.Formats
{
    internal class PngRendererColorFrame : PngRendererFrame
    {
        private static readonly byte[] s_divide255 = Enumerable.Range(0, 256 * 256).Select(i => (byte)(i / 255)).ToArray();
        private readonly IDATStream _data;
        private readonly bool _hasAlpha;
        private readonly HashSet<int> _transparencyColor;
        private readonly byte[] _line;
        private readonly int _stride;

        public PngRendererColorFrame(ApngFile file, ApngFrame frame, TimeSpan begin) : base(file, frame, begin)
        {

            _data = new IDATStream(file, frame);

            if (file.tRNSChunk is null)
            {
                _transparencyColor = new HashSet<int>();
            }
            else
            {
                var trns = (tRNSColorChunk)file.tRNSChunk;
                var colcodes = trns.TransparencyColors
                                   .Select(col => (col.R << 16) | (col.G << 8) | col.B);

                _transparencyColor = new HashSet<int>(colcodes);
            }

            _line = new byte[_data.Stride];
            _hasAlpha = file.IHDRChunk.ColorType == ColorType.ColorAlpha;
            _stride = Width * 4;
        }

        public override void Render(IBitmapFace bitmap, byte[] work, byte[]? backup)
        {
            bitmap.ReadBGRA(work, X, Y, Width, Height);

            if (backup != null)
            {
                Buffer.BlockCopy(work, 0, backup, 0, _stride * Height);
            }

            RenderBlock(work);

            bitmap.WriteBGRA(work, X, Y, Width, Height);
        }

        public override async Task RenderAsync(IBitmapFace bitmap, byte[] work, byte[]? backup)
        {
            bitmap.ReadBGRA(work, X, Y, Width, Height);
            if (backup is not null)
            {
                Buffer.BlockCopy(work, 0, backup, 0, _stride * Height);
            }

            await Task.Factory.StartNew(
                s => RenderBlock((byte[])s!),
                work,
                System.Threading.CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default
            ).ConfigureAwait(false);

            bitmap.WriteBGRA(work, X, Y, Width, Height);
        }

        private void RenderBlock(byte[] work)
        {
            if (_hasAlpha)
            {
                switch (BlendMethod)
                {
                    case BlendOps.APNGBlendOpSource:
                        RenderBlockOpSourceAlpha(work);
                        break;
                    case BlendOps.APNGBlendOpOver:
                        RenderBlockOpOverAlpha(work);
                        break;
                }
            }
            else
            {
                RenderBlockNoAlpha(work);
            }
        }

        private void RenderBlockOpSourceAlpha(byte[] work)
        {
            int workIdx = 0;
            int stride = _stride;
            for (var i = 0; i < Height; ++i)
            {
                _data.DecompressLine(_line, 0, _line.Length);

                int lineIdx = 0;
                int workEdIdx = workIdx + stride;

                while (workIdx < workEdIdx)
                {
                    var r = _line[lineIdx++];
                    var g = _line[lineIdx++];
                    var b = _line[lineIdx++];
                    var alpha = _line[lineIdx++];

                    switch (alpha)
                    {
                        case 0:
                            workIdx += 4;
                            break;

                        case 255:
                            work[workIdx++] = b;
                            work[workIdx++] = g;
                            work[workIdx++] = r;
                            work[workIdx++] = 255;
                            break;

                        default:
                            work[workIdx++] = s_divide255[(alpha * b + 127) & 0xFFFF];
                            work[workIdx++] = s_divide255[(alpha * g + 127) & 0xFFFF];
                            work[workIdx++] = s_divide255[(alpha * r + 127) & 0xFFFF];
                            work[workIdx++] = alpha;
                            break;
                    }
                }
            }
            _data.Reset();
        }

        private void RenderBlockOpOverAlpha(byte[] work)
        {
            int workIdx = 0;
            int stride = _stride;
            for (var i = 0; i < Height; ++i)
            {
                _data.DecompressLine(_line, 0, _line.Length);

                int lineIdx = 0;
                int workEdIdx = workIdx + stride;

                while (workIdx < workEdIdx)
                {
                    var r = _line[lineIdx++];
                    var g = _line[lineIdx++];
                    var b = _line[lineIdx++];
                    var alpha = _line[lineIdx++];

                    switch (alpha)
                    {
                        case 0:
                            workIdx += 4;
                            break;

                        case 255:
                            work[workIdx++] = b;
                            work[workIdx++] = g;
                            work[workIdx++] = r;
                            work[workIdx++] = 255;
                            break;

                        default:
                            work[workIdx] = ComputeColorScale(alpha, b, work[workIdx]); ++workIdx;
                            work[workIdx] = ComputeColorScale(alpha, g, work[workIdx]); ++workIdx;
                            work[workIdx] = ComputeColorScale(alpha, r, work[workIdx]); ++workIdx;
                            work[workIdx] = ComputeAlphaScale(alpha, work[workIdx]);
                            ++workIdx;
                            break;
                    }
                }
            }
            _data.Reset();
        }

        private void RenderBlockNoAlpha(byte[] work)
        {
            int workIdx = 0;
            int stride = _stride;
            for (var i = 0; i < Height; ++i)
            {
                _data.DecompressLine(_line, 0, _line.Length);

                int lineIdx = 0;
                int workEdIdx = workIdx + stride;

                while (workIdx < workEdIdx)
                {
                    var r = _line[lineIdx++];
                    var g = _line[lineIdx++];
                    var b = _line[lineIdx++];

                    if (_transparencyColor.Contains((r << 16) | (g << 8) | b))
                    {
                        workIdx += 4;
                    }
                    else
                    {
                        work[workIdx++] = b;
                        work[workIdx++] = g;
                        work[workIdx++] = r;
                        work[workIdx++] = 255;
                    }
                }
            }
            _data.Reset();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte ComputeColorScale(byte sa, byte sv, byte dv)
        {
            var val = sa * sv + (255 - sa) * dv;
            return s_divide255[(val + 127) & 0xFFFF];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte ComputeAlphaScale(byte sa, byte dv)
        {
            // work[workIdx] = (byte)(alpha + work[workIdx] * (255 - alpha) / 255);
            var val = s_divide255[((255 - sa) * dv + 127) & 0xFFFF];
            return (byte)(sa + val);
        }
    }
}
