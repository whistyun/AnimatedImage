using AnimatedImage.Formats.Png.Chunks;
using AnimatedImage.Formats.Png.Types;
using AnimatedImage.Formats.Png;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimatedImage.Formats
{
    internal class PngRendererIndexedFrame : PngRendererFrame
    {
        private IDATStream _data;
        private PngColor[] _palette;
        private byte[] _transparency;

        private byte[]? _decompress;

        public PngRendererIndexedFrame(ApngFile file, Png.ApngFrame frame, TimeSpan begin) : base(file, frame, begin)
        {
            if (file.PLTEChunk is null)
                throw new ArgumentNullException("file.PLTEChunk is null");

            _data = new IDATStream(file, frame);
            _palette = file.PLTEChunk.Colors;

            if (file.tRNSChunk is null)
            {
                _transparency = new byte[0];
            }
            else
            {
                var trns = (tRNSIndexChunk)file.tRNSChunk;
                _transparency = trns.AlphaForEachIndex;
            }

            if (_transparency.Length < _palette.Length)
            {
                _transparency = _transparency.Concat(Enumerable.Repeat((byte)0xFF, _palette.Length - _transparency.Length))
                                             .ToArray();
            }
        }

        public override void Render(IBitmapFace bitmap, byte[] work, byte[]? backup)
        {
            if (_decompress is null)
            {
                Decompress();
            }

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
            var dcTsk = _decompress is null ? Task.Run(Decompress) : Task.FromResult(0);

            bitmap.ReadBGRA(work, X, Y, Width, Height);
            if (backup is not null)
                Array.Copy(work, backup, Width * Height * 4);

            await dcTsk.ContinueWith((tsk, s) => RenderBlock((byte[])s), work);

            bitmap.WriteBGRA(work, X, Y, Width, Height);
        }

        private void Decompress()
        {
            _decompress = new byte[_data.Stride * Height];

            var i = 0;
            while (i < _decompress.Length)
            {
                _data.DecompressLine(_decompress, i, _data.Stride);
                i += _data.Stride;
            }
        }

        private void RenderBlock(byte[] work) {
            if (BlendMethod == BlendOps.APNGBlendOpSource)
            {
                int j = 0;
                for (var i = 0; i < _decompress.Length; ++i)
                {
                    var idx = _decompress[i];
                    var color = _palette[idx];
                    work[j++] = color.B;
                    work[j++] = color.G;
                    work[j++] = color.R;
                    work[j++] = _transparency[idx];
                }
            }
            else if (BlendMethod == BlendOps.APNGBlendOpOver)
            {
                int j = 0;
                for (var i = 0; i < _decompress.Length; ++i)
                {
                    var idx = _decompress[i];
                    var alpha = _transparency[idx];

                    if (alpha == 0)
                    {
                        j += 4;
                        continue;
                    }

                    var color = _palette[idx];

                    if (alpha == 0xFF)
                    {
                        work[j++] = color.B;
                        work[j++] = color.G;
                        work[j++] = color.R;
                        work[j++] = alpha;
                    }
                    else
                    {
                        work[j] = (byte)((alpha * color.B + (1 - alpha) * work[j]) >> 8); ++j;
                        work[j] = (byte)((alpha * color.G + (1 - alpha) * work[j]) >> 8); ++j;
                        work[j] = (byte)((alpha * color.R + (1 - alpha) * work[j]) >> 8); ++j;
                        work[j] = alpha;
                        ++j;
                    }
                }
            }
        }
    }
}
