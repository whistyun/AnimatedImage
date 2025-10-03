using AnimatedImage.Formats.Png.Chunks;
using AnimatedImage.Formats.Png.Types;
using AnimatedImage.Formats.Png;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnimatedImage.Formats
{
    internal class PngRendererColorFrame : PngRendererFrame
    {
        private readonly IDATStream _data;
        private readonly bool _hasAlpha;
        private readonly HashSet<PngColor> _transparencyColor;
        private readonly byte[] _line;

        public PngRendererColorFrame(ApngFile file, Png.ApngFrame frame, TimeSpan begin) : base(file, frame, begin)
        {
            _data = new IDATStream(file, frame);


            if (file.tRNSChunk is null)
            {
                _transparencyColor = new HashSet<PngColor>();
            }
            else
            {
                var trns = (tRNSColorChunk)file.tRNSChunk;
                _transparencyColor = new HashSet<PngColor>(trns.TransparencyColors);
            }

            _line = new byte[_data.Stride];
            _hasAlpha = file.IHDRChunk.ColorType == ColorType.ColorAlpha;
        }

        public override void Render(IBitmapFace bitmap, byte[] work, byte[]? backup)
        {
            bitmap.ReadBGRA(work, X, Y, Width, Height);

            if (backup != null)
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

            await Task.Factory.StartNew(w => RenderBlock((byte[])w!), work);

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
                    var r = _line[lineIdx++];
                    var g = _line[lineIdx++];
                    var b = _line[lineIdx++];
                    var alpha =
                        _hasAlpha ? _line[lineIdx++] :
                        _transparencyColor.Contains(new PngColor(r, g, b)) ? (byte)0 :
                        (byte)255;

                    if (BlendMethod == BlendOps.APNGBlendOpSource)
                    {
                        work[workIdx++] = b;
                        work[workIdx++] = g;
                        work[workIdx++] = r;
                        work[workIdx++] = alpha;
                    }
                    else if (BlendMethod == BlendOps.APNGBlendOpOver)
                    {
                        if (alpha == 0)
                        {
                            workIdx += 4;
                        }
                        else if (alpha == 0xFF)
                        {
                            work[workIdx++] = b;
                            work[workIdx++] = g;
                            work[workIdx++] = r;
                            work[workIdx++] = alpha;
                        }
                        else
                        {
                            work[workIdx] = ComputeColorScale(alpha, b, work[workIdx]); ++workIdx;
                            work[workIdx] = ComputeColorScale(alpha, g, work[workIdx]); ++workIdx;
                            work[workIdx] = ComputeColorScale(alpha, r, work[workIdx]); ++workIdx;
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
