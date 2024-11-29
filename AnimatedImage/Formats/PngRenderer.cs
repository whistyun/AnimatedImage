using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AnimatedImage.Formats.Png;
using AnimatedImage.Formats.Png.Chunks;
using AnimatedImage.Formats.Png.Types;

namespace AnimatedImage.Formats
{
    internal class PngRenderer : FrameRenderer
    {
        private IBitmapFaceFactory _factory;
        private ApngFile _file;
        private int _frameIndex = -1;
        private readonly IBitmapFace _bitmap;
        private readonly PngRendererFrame[] _frames;

        private readonly byte[] _work;

        // variables for RestorePrevious
        private readonly byte[] _restorePixels;
        private PngRendererFrame? _previouns;

        // variables for RestoreBackground
        private FrameRenderFrame? _background;

        public PngRenderer(ApngFile file, IBitmapFaceFactory factory)
        {
            _file = file;
            _factory = factory;
            Width = file.IHDRChunk.Width;
            Height = file.IHDRChunk.Height;
            _bitmap = factory.Create(Width, Height);
            _restorePixels = new byte[Width * Height * 4];
            _work = new byte[Width * Height * 4];

            var frames = new List<PngRendererFrame>();
            var span = TimeSpan.Zero;
            foreach (var pngfrm in file.Frames)
            {
                var frame = CreateFrame(file, pngfrm, span);
                span = frame.End;

                frames.Add(frame);
            }

            if (frames.Count == 0)
            {
                _frames = new[] { CreateFrame(file, file.DefaultImage, span) };
                Duration = _frames[0].End;
            }
            else
            {
                _frames = frames.ToArray();
                Duration = span;
            }

            static PngRendererFrame CreateFrame(ApngFile file, ApngFrame pngfrm, TimeSpan span)
            {
                return file.IHDRChunk.ColorType switch
                {
                    ColorType.Glayscale => new PngRendererGrayscaleFrame(file, pngfrm, span),
                    ColorType.GlayscaleAlpha => new PngRendererGrayscaleFrame(file, pngfrm, span),
                    ColorType.Color => new PngRendererColorFrame(file, pngfrm, span),
                    ColorType.ColorAlpha => new PngRendererColorFrame(file, pngfrm, span),
                    ColorType.IndexedColor => new PngRendererIndexedFrame(file, pngfrm, span),
                    _ => throw new ArgumentException()
                };
            }
        }

        public int Width { get; }

        public int Height { get; }

        public override int CurrentIndex => _frameIndex;

        public override int FrameCount => _frames.Length;

        public override IBitmapFace Current => _bitmap;

        public override int RepeatCount { get; }

        public override FrameRenderFrame this[int idx] => _frames[idx];

        public override TimeSpan Duration { get; }

        public override FrameRenderer Clone()
        {
            return new PngRenderer(_file, _factory);
        }

        public override TimeSpan GetStartTime(int idx) => _frames[idx].Begin;

        public override void ProcessFrame(int frameIndex)
        {
            if (_frameIndex == frameIndex)
                return;

            // increment
            for (; ; )
            {
                var frm = _frames[frameIndex];
                if (frm.Begin == frm.End && frameIndex + 1 < _frames.Length)
                {
                    ++frameIndex;
                    continue;
                }
                break;
            }

            if (_frameIndex > frameIndex)
            {
                _bitmap.Clear(0, 0, Width, Height);
                _frameIndex = 0;
                _previouns = null;
                _background = null;
            }

            // restore

            if (_previouns != null)
            {
                _bitmap.WriteBGRA(_restorePixels, _previouns.X, _previouns.Y, _previouns.Width, _previouns.Height);
                _previouns = null;
            }

            if (_background != null)
            {
                _bitmap.Clear(_background.X, _background.Y, _background.Width, _background.Height);
            }


            // render intermediate frames

            for (var fidx = Math.Max(_frameIndex, 0); fidx < frameIndex; ++fidx)
            {
                var prevFrame = _frames[fidx];

                if (prevFrame.DisposalMethod == DisposeOps.APNGDisposeOpPrevious)
                    continue;

                if (prevFrame.DisposalMethod == DisposeOps.APNGDisposeOpBackground)
                {
                    // skips clear because the cleaning area is already cleared by previous frame.
                    if (_background != null && _background.IsInvolve(prevFrame))
                        continue;

                    _bitmap.Clear(prevFrame.X, prevFrame.Y, prevFrame.Width, prevFrame.Height);
                    if (_background is null || prevFrame.IsInvolve(_background))
                    {
                        _background = prevFrame;
                    }

                    continue;
                }

                prevFrame.Render(_bitmap, _work, null);
            }


            // render current frame
            var curFrame = _frames[frameIndex];

            switch (curFrame.DisposalMethod)
            {
                case DisposeOps.APNGDisposeOpPrevious:
                    curFrame.Render(_bitmap, _work, _restorePixels);
                    _background = null;
                    _previouns = curFrame;
                    break;

                case DisposeOps.APNGDisposeOpBackground:
                    curFrame.Render(_bitmap, _work, null);
                    _background = curFrame;
                    _previouns = null;
                    break;

                default:
                    curFrame.Render(_bitmap, _work, null);
                    _background = null;
                    _previouns = null;
                    break;
            }

            _frameIndex = frameIndex;
        }

        public override async Task ProcessFrameAsync(int frameIndex)
        {
            if (_frameIndex == frameIndex)
                return;

            // increment
            for (; ; )
            {
                var frm = _frames[frameIndex];
                if (frm.Begin == frm.End && frameIndex + 1 < _frames.Length)
                {
                    ++frameIndex;
                    continue;
                }
                break;
            }

            if (_frameIndex > frameIndex)
            {
                _bitmap.Clear(0, 0, Width, Height);
                _frameIndex = 0;
                _previouns = null;
                _background = null;
            }

            // restore

            if (_previouns != null)
            {
                _bitmap.WriteBGRA(_restorePixels, _previouns.X, _previouns.Y, _previouns.Width, _previouns.Height);
                _previouns = null;
            }

            if (_background != null)
            {
                _bitmap.Clear(_background.X, _background.Y, _background.Width, _background.Height);
            }

            // render intermediate frames

            for (var fidx = Math.Max(_frameIndex, 0); fidx < frameIndex; ++fidx)
            {
                var prevFrame = _frames[fidx];

                if (prevFrame.DisposalMethod == DisposeOps.APNGDisposeOpPrevious)
                    continue;

                if (prevFrame.DisposalMethod == DisposeOps.APNGDisposeOpBackground)
                {
                    // skips clear because the cleaning area is already cleared by previous frame.
                    if (_background != null && _background.IsInvolve(prevFrame))
                        continue;

                    _bitmap.Clear(prevFrame.X, prevFrame.Y, prevFrame.Width, prevFrame.Height);
                    if (_background is null || prevFrame.IsInvolve(_background))
                    {
                        _background = prevFrame;
                    }

                    continue;
                }

                await prevFrame.RenderAsync(_bitmap, _work, null);
            }

            // render current frame
            var curFrame = _frames[frameIndex];

            switch (curFrame.DisposalMethod)
            {
                case DisposeOps.APNGDisposeOpPrevious:
                    await curFrame.RenderAsync(_bitmap, _work, _restorePixels);
                    _background = null;
                    _previouns = curFrame;
                    break;

                case DisposeOps.APNGDisposeOpBackground:
                    await curFrame.RenderAsync(_bitmap, _work, null);
                    _background = curFrame;
                    _previouns = null;
                    break;

                default:
                    await curFrame.RenderAsync(_bitmap, _work, null);
                    _background = null;
                    _previouns = null;
                    break;
            }

            _frameIndex = frameIndex;
        }
    }
}
