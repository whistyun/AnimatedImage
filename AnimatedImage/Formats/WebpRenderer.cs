using AnimatedImage.Formats.Png;
using AnimatedImage.Formats.Png.Chunks;
using AnimatedImage.Formats.WebP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static AnimatedImage.Formats.WebP.WebpWrapper;

namespace AnimatedImage.Formats
{
    internal class WebpRenderer : FrameRenderer
    {
        public static bool CheckSupport() => WebpWrapper.CheckSupport();

        private readonly WebpRendererFrame[] _frames;
        private IBitmapFaceFactory _factory;
        private IBitmapFace _bitmap;

        private byte[] _rawWebp;
        GCHandle _pinnedWebP;
        private WebPAnimDecoder _decoder;

        private int _last_access = -1;
        private bool disposedValue;

        public WebpRenderer(Stream stream, IBitmapFaceFactory factory)
        {
            if (!CheckSupport())
            {
                throw new InvalidOperationException("AnimatedImage.Native required");
            }

            _rawWebp = stream.ReadBytes((int)stream.Length);
            InitWebp(_rawWebp);

            WebPAnimInfo anim_info = new WebPAnimInfo();
            WebpWrapper.WebPAnimDecoderGetInfo(_decoder.decoder, out anim_info);

            Width = (int)anim_info.canvas_width;
            Height = (int)anim_info.canvas_height;

            _bitmap = factory.Create(Width, Height);

            FrameCount = (int)anim_info.frame_count;
            RepeatCount = (int)anim_info.loop_count;

            var frames = new List<WebpRendererFrame>();

            IntPtr demuxer = WebpWrapper.WebPAnimDecoderGetDemuxer(_decoder);
            WebPIterator iter = new WebPIterator();
            TimeSpan begin = TimeSpan.Zero;
            if (WebpWrapper.WebPDemuxGetFrame(demuxer, 1, ref iter))
            {
                do
                {
                    var duration = Math.Max(iter.duration, 100);
                    TimeSpan end = begin + TimeSpan.FromMilliseconds(duration);
                    var frame = new WebpRendererFrame(0, 0, Width, Height, begin, end);
                    frames.Add(frame);
                    begin = end;
                } while (WebpWrapper.WebPDemuxNextFrame(ref iter));
                WebpWrapper.WebPDemuxReleaseIterator(ref iter);
            }
            WebpWrapper.WebPAnimDecoderReset(_decoder);
            Duration = begin;

            _frames = frames.ToArray();

            _factory = factory;
        }

        private WebpRenderer(WebpRenderer renderer)
        {
            _rawWebp = renderer._rawWebp;

            Width = renderer.Width;
            Height = renderer.Height;

            _factory = renderer._factory;
            _bitmap = _factory.Create(Width, Height);
            _frames = renderer._frames.ToArray();

            InitWebp(renderer._rawWebp);

            Duration = renderer.Duration;
            FrameCount = renderer.FrameCount;
            RepeatCount = renderer.RepeatCount;
        }


        private void InitWebp(byte[] rawWebp)
        {
            _rawWebp = rawWebp;
            _pinnedWebP = GCHandle.Alloc(_rawWebp, GCHandleType.Pinned);

            WebPAnimDecoderOptions dec_options = new WebPAnimDecoderOptions();
            var result = WebpWrapper.WebPAnimDecoderOptionsInit(ref dec_options);
            dec_options.color_mode = WEBP_CSP_MODE.MODE_BGRA;
            dec_options.use_threads = 1;
            WebPData webp_data = new WebPData
            {
                data = _pinnedWebP.AddrOfPinnedObject(),
                size = new UIntPtr((uint)_rawWebp.Length)
            };
            _decoder = WebpWrapper.WebPAnimDecoderNew(ref webp_data, ref dec_options);

        }

        public int Width { get; }

        public int Height { get; }

        public override FrameRenderFrame this[int frameIndex] => _frames[frameIndex];

        public override int CurrentIndex => _last_access;

        public override int FrameCount { get; }

        public override int RepeatCount { get; }

        public override TimeSpan Duration { get; }

        public override IBitmapFace Current => _bitmap;

        public override FrameRenderer Clone()
        {
            return new WebpRenderer(this);
        }

        public override TimeSpan GetStartTime(int idx) => _frames[idx].Begin;

        private byte[] DecodeFrame(int skipFrameCount)
        {
            int timestamp = 0;
            IntPtr buf = IntPtr.Zero;
            for (int i = 0; i < skipFrameCount; ++i)
            {
                if (!WebPAnimDecoderHasMoreFrames(_decoder))
                {
                    throw new Exception("WebPAnimDecoderHasMoreFrames: error");
                }
                _last_access++;

                if (!WebPAnimDecoderGetNext(_decoder.decoder, ref buf, ref timestamp))
                {
                    throw new Exception("WebPAnimDecoderGetNext: error");
                }
            }
            var data = new byte[Width * Height * 4];
            Marshal.Copy(buf, data, 0, data.Length);
            return data;
        }

        public override void ProcessFrame(int frameIndex)
        {
            if (frameIndex == _last_access)
            {
                return;
            }

            if (_last_access >= frameIndex)
            {
                WebPAnimDecoderReset(_decoder);
                _last_access = -1;
            }

            int count = frameIndex - _last_access;
            var data = DecodeFrame(count);

            _bitmap.WriteBGRA(data, 0, 0, Width, Height);
        }

        public override async Task ProcessFrameAsync(int frameIndex)
        {
            if (frameIndex == _last_access)
            {
                return;
            }

            if (_last_access >= frameIndex)
            {
                WebPAnimDecoderReset(_decoder);
                _last_access = -1;
            }

            int count = frameIndex - _last_access;
            var data = await Task.Run(() => DecodeFrame(count));

            _bitmap.WriteBGRA(data, 0, 0, Width, Height);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: Discard managed state (managed object)
                }

                // TODO: Release unmanaged resources (unmanaged objects) and override finalizers.
                WebPAnimDecoderDelete(_decoder);

                if (_pinnedWebP.IsAllocated)
                {
                    _pinnedWebP.Free();
                }

                // TODO: Set large fields to null.
                disposedValue = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        // TODO: Only override the finalizer if the ‘Dispose(bool disposing)’ contains code that releases unmanaged resources.
        ~WebpRenderer()
        {
            // Do not modify this code. Write cleanup code in the ‘Dispose(bool disposing)’ method.
            Dispose(disposing: false);
        }
    }
}
