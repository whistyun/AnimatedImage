using AnimatedImage.Formats.Png.Chunks;
using AnimatedImage.Formats.Png.Types;
using AnimatedImage.Formats.Png;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnimatedImage.Formats
{
    internal class PngRendererGrayscaleFrame : PngRendererFrame
    {
        private static readonly byte[] s_scle1 = { 0, 255 };
        private static readonly byte[] s_scle2 = Enumerable.Range(0, 4).Select(i => (byte)(i * 255 / 3)).ToArray();
        private static readonly byte[] s_scle4 = Enumerable.Range(0, 16).Select(i => (byte)(i * 255 / 15)).ToArray();
        private static readonly byte[] s_scle8 = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        private readonly IDATStream _data;
        private readonly bool _hasAlpha;
        private readonly byte[] _alphaLevel;
        private readonly byte[] _line;
        private readonly byte[] _scale;

        public PngRendererGrayscaleFrame(ApngFile file, Png.ApngFrame frame, TimeSpan begin) : base(file, frame, begin)
        {
            _data = new IDATStream(file, frame);

            if (file.tRNSChunk is null)
            {
                _alphaLevel = new byte[256];
                for (var i = 0; i < _alphaLevel.Length; ++i)
                    _alphaLevel[i] = 255;
            }
            else
            {
                var trns = (tRNSGrayscaleChunk)file.tRNSChunk;
                _alphaLevel = trns.AlphaForEachGrayLevel.Select(s => (byte)(s >> 8)).ToArray();
            }

            _line = new byte[_data.Stride];
            _hasAlpha = file.IHDRChunk.ColorType == ColorType.GlayscaleAlpha;

            _scale = file.IHDRChunk.BitDepth switch
            {
                1 => s_scle1,
                2 => s_scle2,
                4 => s_scle4,
                8 => s_scle8,
                16 => s_scle8,
                _ => throw new ArgumentException()
            };
        }

        public override void Render(IBitmapFace bitmap, byte[] work, byte[]? backup)
        {
            bitmap.ReadBGRA(work, X, Y, Width, Height);
            if (backup is not null)
            {
                Array.Copy(work, backup, Width * Height * 4);
            }

            RenderBlock(work);

            bitmap.WriteBGRA(work, X, Y, Width, Height);
        }

        public override async Task RenderAsync(IBitmapFace bitmap, byte[] work, byte[]? backup)
        {
            bitmap.ReadBGRA(work, X, Y, Width, Height);
            if (backup is not null)
            {
                Array.Copy(work, backup, Width * Height * 4);
            }

            await Task.Factory.StartNew(w => RenderBlock((byte[])w), work);

            bitmap.WriteBGRA(work, X, Y, Width, Height);
        }

        private void RenderBlock(byte[] work)
        {
            int workIdx = 0;
            for (var i = 0; i < Height; ++i)
            {
                _data.DecompressLine(_line, 0, _line.Length);

                int lineIdx = 0;
                int workEdIdx = workIdx + Width * 4;
                while (workIdx < workEdIdx)
                {
                    var val = _line[lineIdx++];
                    var alpha = _hasAlpha ? _line[lineIdx++] : _alphaLevel[val];

                    var scl = _scale[val];

                    if (BlendMethod == BlendOps.APNGBlendOpSource)
                    {
                        work[workIdx++] = scl;
                        work[workIdx++] = scl;
                        work[workIdx++] = scl;
                        work[workIdx++] = alpha;
                    }
                    else if (BlendMethod == BlendOps.APNGBlendOpOver)
                    {

                        if (alpha == 0)
                        {
                            workIdx += 4;
                            continue;
                        }
                        else if (alpha == 0xFF)
                        {
                            work[workIdx++] = scl;
                            work[workIdx++] = scl;
                            work[workIdx++] = scl;
                            work[workIdx++] = alpha;
                        }
                        else
                        {
                            var overVal = ComputeColorScale(alpha, scl, work[workIdx]); ;
                            work[workIdx++] = overVal;
                            work[workIdx++] = overVal;
                            work[workIdx++] = overVal;
                            work[workIdx] = ComputeAlphaScale(alpha, work[workIdx]);
                            ++workIdx;
                        }
                    }
                }
            }
            _data.Reset();
        }

        static byte ComputeColorScale(byte sa, byte sv, byte dv)
        {
            var val = sa * sv + (255 - sa) * dv;
            val = (val * 2 + 255) / 255 / 2;
            return (byte)val;
        }

        static byte ComputeAlphaScale(byte sa, byte dv)
        {
            // work[workIdx] = (byte)(alpha + work[workIdx] * (255 - alpha) / 255);
            var val = ((255 - sa) * dv * 2 + 255) / 255 / 2;
            return (byte)(sa + val);
        }
    }
}
