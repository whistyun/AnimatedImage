using AnimatedImage.Formats.Gif;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimatedImage.Formats
{
    internal class GifRendererFrame : FrameRenderFrame
    {
        public FrameDisposalMethod DisposalMethod { get; }

        private readonly GifColor[] _colorTable;
        private readonly GifImageData _data;
        private readonly int _transparencyIndex;
        private byte[]? _decompress;
        private readonly bool _interlace;

        public GifRendererFrame(
                GifFile file, GifFrame frame,
                TimeSpan begin, TimeSpan end,
                FrameDisposalMethod method,
                int transparencyIndex)
            : base(frame.Descriptor.Left, frame.Descriptor.Top,
                   frame.Descriptor.Width, frame.Descriptor.Height,
                   begin, end)
        {
            _colorTable = frame.LocalColorTable
                       ?? file.GlobalColorTable
                       ?? throw new FormatException("ColorTable not found");
            _data = frame.ImageData;
            _transparencyIndex = transparencyIndex;
            _interlace = frame.Descriptor.Interlace;

            DisposalMethod = method;
        }

        public void Render(IBitmapFace bitmap, byte[] work, byte[]? backup)
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

        public async Task RenderAsync(IBitmapFace bitmap, byte[] work, byte[]? backup)
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
            _decompress = _data.Decompress();

            for (var i = 0; i < _decompress.Length; ++i)
            {
                if (_decompress[i] >= _colorTable.Length)
                    _decompress[i] = 0;
            }
        }

        private void RenderBlock(byte[] work)
        {
            if (_interlace)
            {
                int i = 0;
                i += RenderInterlace(work, i, 0, 8);
                i += RenderInterlace(work, i, 4, 8);
                i += RenderInterlace(work, i, 2, 4);
                i += RenderInterlace(work, i, 1, 2);
            }
            else
            {
                RenderFull(work);
            }
        }

        private int RenderInterlace(byte[] work, int startLine, int start, int stepLine)
        {
            if (_decompress == null) throw new InvalidOperationException();

            int i = 0;
            for (int y = start; y < Height; y += stepLine)
            {
                for (int x = 0; x < Width; x++)
                {
                    var pos = y * Width + x;

                    var idx = _decompress[startLine + i];
                    i++;

                    if (idx == _transparencyIndex)
                    {
                        continue;
                    }

                    var color = _colorTable[idx];
                    work[4 * pos + 0] = color.B;
                    work[4 * pos + 1] = color.G;
                    work[4 * pos + 2] = color.R;
                    work[4 * pos + 3] = 255;
                }
            }
            return i;
        }

        private unsafe void RenderFull(byte[] work)
        {
            if (_decompress == null) throw new InvalidOperationException();

            fixed (byte* workPtr0 = &work[0])
            fixed (GifColor* palette = &_colorTable[0])
            {
                byte* workPtr = workPtr0;

                for (var i = 0; i < _decompress.Length; ++i)
                {
                    var idx = _decompress[i];

                    if (idx != _transparencyIndex)
                    {
                        var color = palette + idx;
                        int cval = *(int*)color;
                        *(int*)workPtr = cval;
                    }
                    workPtr += 4;
                }
            }
        }

        public static GifRendererFrame Create(GifFile file, GifFrame frame, TimeSpan begin)
        {
            var gce = frame.Extensions
                           .OfType<GifGraphicControlExtension>()
                           .FirstOrDefault();

            TimeSpan end;
            FrameDisposalMethod method;
            int transparencyIndex;

            if (gce is null)
            {
                end = begin + TimeSpan.FromMilliseconds(100);
                method = FrameDisposalMethod.None;
                transparencyIndex = -1;
            }
            else
            {
                end = begin + TimeSpan.FromMilliseconds(gce.Delay < 20 ? 100 : gce.Delay);
                method = (FrameDisposalMethod)gce.DisposalMethod;
                transparencyIndex = gce.HasTransparency ? gce.TransparencyIndex : -1;
            }

            return new GifRendererFrame(file, frame, begin, end, method, transparencyIndex);
        }
    }
}
