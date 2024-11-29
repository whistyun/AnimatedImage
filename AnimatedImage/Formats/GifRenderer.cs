using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnimatedImage.Formats.Gif;

namespace AnimatedImage.Formats
{
    internal class GifRenderer : FrameRenderer
    {
        private IBitmapFaceFactory _factory;
        private int _frameIndex = -1;
        private readonly IBitmapFace _bitmap;
        private readonly GifRendererFrame[] _frames;

        private readonly byte[] _work;

        // variables for RestorePrevious
        private readonly byte[] _restorePixels;
        private GifRendererFrame? _previouns;

        // variables for RestoreBackground
        private FrameRenderFrame? _background;
        private readonly FrameRenderFrame _fullFrame;

        public GifRenderer(GifFile file, IBitmapFaceFactory factory)
        {
            var descriptor = file.Header.LogicalScreenDescriptor;
            Width = descriptor.Width;
            Height = descriptor.Height;

            _factory = factory;
            _fullFrame = _background = new FrameRenderFrame(0, 0, Width, Height, TimeSpan.Zero, TimeSpan.Zero);

            _bitmap = _factory.Create(Width, Height);
            _restorePixels = new byte[Width * Height * 4];
            _work = new byte[Width * Height * 4];

            var frames = new List<GifRendererFrame>();
            var span = TimeSpan.Zero;
            foreach (var giffrm in file.Frames)
            {
                var frame = GifRendererFrame.Create(file, giffrm, span);
                span = frame.End;

                frames.Add(frame);
            }

            _frames = frames.ToArray();

            Duration = span;
            FrameCount = file.Frames.Count;
            RepeatCount = file.RepeatCount;
        }

        private GifRenderer(GifRenderer renderer)
        {
            Width = renderer.Width;
            Height = renderer.Height;

            _factory = renderer._factory;
            _fullFrame = _background = new FrameRenderFrame(0, 0, Width, Height, TimeSpan.Zero, TimeSpan.Zero);
            _bitmap = _factory.Create(Width, Height);
            _restorePixels = new byte[Width * Height * 4];
            _work = new byte[Width * Height * 4];
            _frames = renderer._frames.ToArray();

            Duration = renderer.Duration;
            FrameCount = renderer.FrameCount;
            RepeatCount = renderer.RepeatCount;
        }

        public int Width { get; }

        public int Height { get; }

        public override int CurrentIndex => _frameIndex;

        public override int FrameCount { get; }

        public override IBitmapFace Current => _bitmap;

        public override TimeSpan Duration { get; }

        public override int RepeatCount { get; }

        public override FrameRenderFrame this[int idx] => _frames[idx];

        public override void ProcessFrame(int frameIndex)
        {
            if (_frameIndex == frameIndex)
                return;

            if (_frameIndex > frameIndex)
            {
                _frameIndex = -1;
                _previouns = null;
                _background = _fullFrame;
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

            for (var fidx = _frameIndex + 1; fidx < frameIndex; ++fidx)
            {
                var prevFrame = _frames[fidx];

                if (prevFrame.DisposalMethod == FrameDisposalMethod.RestorePrevious)
                {
                    _background = null;
                    continue;
                }

                if (prevFrame.DisposalMethod == FrameDisposalMethod.RestoreBackground)
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
                _background = null;
            }


            // render current frame

            var curFrame = _frames[frameIndex];

            switch (curFrame.DisposalMethod)
            {
                case FrameDisposalMethod.RestorePrevious:
                    curFrame.Render(_bitmap, _work, _restorePixels);
                    _background = null;
                    _previouns = curFrame;
                    break;

                case FrameDisposalMethod.RestoreBackground:
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

            if (_frameIndex > frameIndex)
            {
                _frameIndex = -1;
                _previouns = null;
                _background = _fullFrame;
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

            for (var fidx = _frameIndex + 1; fidx < frameIndex; ++fidx)
            {
                var prevFrame = _frames[fidx];

                if (prevFrame.DisposalMethod == FrameDisposalMethod.RestorePrevious)
                {
                    _background = null;
                    continue;
                }

                if (prevFrame.DisposalMethod == FrameDisposalMethod.RestoreBackground)
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
                _background = null;
            }

            // render current frame

            var curFrame = _frames[frameIndex];

            switch (curFrame.DisposalMethod)
            {
                case FrameDisposalMethod.RestorePrevious:
                    await curFrame.RenderAsync(_bitmap, _work, _restorePixels);
                    _background = null;
                    _previouns = curFrame;
                    break;

                case FrameDisposalMethod.RestoreBackground:
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

        public override TimeSpan GetStartTime(int idx) => _frames[idx].Begin;

        public override FrameRenderer Clone() => new GifRenderer(this);
    }
}
